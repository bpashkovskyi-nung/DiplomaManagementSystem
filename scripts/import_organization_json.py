#!/usr/bin/env python3
"""One-off import: faculties, departments, employees from NUNG JSON into PostgreSQL."""

from __future__ import annotations

import json
import os
import sys
import uuid
from datetime import datetime, timezone
from pathlib import Path

import psycopg2
import psycopg2.extras

# Connection string is read from the DIPLOMA_IMPORT_PG environment variable, e.g.:
#   host=<host> dbname=<db> user=<user> password=<pwd> sslmode=require
CONN = os.environ.get("DIPLOMA_IMPORT_PG")

EMPLOYEE_ROLE = "Employee"
USER_KIND_EMPLOYEE = 1


def norm_email(email: str | None) -> str | None:
    if email is None:
        return None
    value = email.strip().lower()
    return value or None


def main() -> int:
    json_path = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(
        r"d:\Downloads\faculties_departments_employees_2025_2026_with_emails.json"
    )
    if not CONN:
        print(
            "Set the DIPLOMA_IMPORT_PG environment variable with the connection string.",
            file=sys.stderr,
        )
        return 1

    if not json_path.is_file():
        print(f"File not found: {json_path}", file=sys.stderr)
        return 1

    with json_path.open(encoding="utf-8") as f:
        payload = json.load(f)

    faculties = payload.get("faculties_and_institutes") or []
    if not faculties:
        print("No faculties_and_institutes in JSON", file=sys.stderr)
        return 1

    now = datetime.now(timezone.utc)
    stats = {
        "faculties_created": 0,
        "faculties_existing": 0,
        "departments_created": 0,
        "departments_existing": 0,
        "users_created": 0,
        "users_existing": 0,
        "dept_employees_created": 0,
        "dept_employees_existing": 0,
        "skipped_no_email": 0,
        "roles_assigned": 0,
    }

    with psycopg2.connect(CONN) as conn:
        conn.autocommit = False
        with conn.cursor(cursor_factory=psycopg2.extras.DictCursor) as cur:
            cur.execute('SELECT "Id" FROM "AspNetRoles" WHERE "Name" = %s', (EMPLOYEE_ROLE,))
            role_row = cur.fetchone()
            if role_row is None:
                print(f"Role {EMPLOYEE_ROLE} not found in AspNetRoles", file=sys.stderr)
                return 1
            employee_role_id = role_row["Id"]

            cur.execute('SELECT "Id", "NormalizedEmail" FROM users')
            users_by_email = {
                row["NormalizedEmail"]: row["Id"]
                for row in cur.fetchall()
                if row["NormalizedEmail"]
            }

            cur.execute('SELECT "Id", "Name" FROM faculties')
            faculties_by_name = {row["Name"].strip().lower(): row["Id"] for row in cur.fetchall()}

            cur.execute('SELECT "Id", "FacultyId", "Name" FROM departments')
            departments_by_key: dict[tuple[uuid.UUID, str], uuid.UUID] = {}
            for row in cur.fetchall():
                departments_by_key[(row["FacultyId"], row["Name"].strip().lower())] = row["Id"]

            cur.execute(
                'SELECT "DepartmentId", "UserId" FROM department_employees WHERE "IsActive" = TRUE'
            )
            existing_memberships = {(row["DepartmentId"], row["UserId"]) for row in cur.fetchall()}

            cur.execute('SELECT "UserId", "RoleId" FROM "AspNetUserRoles"')
            existing_user_roles = {(row["UserId"], row["RoleId"]) for row in cur.fetchall()}

            for faculty_data in faculties:
                faculty_name = (faculty_data.get("name") or "").strip()
                if not faculty_name:
                    continue

                key = faculty_name.lower()
                faculty_id = faculties_by_name.get(key)
                if faculty_id is None:
                    faculty_id = uuid.uuid4()
                    cur.execute(
                        """
                        INSERT INTO faculties ("Id", "Name", "IsActive", "CreatedAt")
                        VALUES (%s, %s, TRUE, %s)
                        """,
                        (str(faculty_id), faculty_name, now),
                    )
                    faculties_by_name[key] = faculty_id
                    stats["faculties_created"] += 1
                else:
                    stats["faculties_existing"] += 1

                for dept_data in faculty_data.get("departments") or []:
                    dept_name = (dept_data.get("name") or "").strip()
                    if not dept_name:
                        continue

                    dept_key = (faculty_id, dept_name.lower())
                    department_id = departments_by_key.get(dept_key)
                    if department_id is None:
                        department_id = uuid.uuid4()
                        cur.execute(
                            """
                            INSERT INTO departments (
                                "Id", "FacultyId", "Name", "SpecialtyCode", "SpecialtyName",
                                "StudyForm", "IsActive", "CreatedAt")
                            VALUES (%s, %s, %s, '', '', '', TRUE, %s)
                            """,
                            (str(department_id), str(faculty_id), dept_name, now),
                        )
                        departments_by_key[dept_key] = department_id
                        stats["departments_created"] += 1
                    else:
                        stats["departments_existing"] += 1

                    for emp in dept_data.get("employees") or []:
                        email = norm_email(emp.get("email"))
                        if email is None:
                            stats["skipped_no_email"] += 1
                            continue

                        full_name = (emp.get("full_name") or "").strip()
                        if not full_name:
                            continue

                        normalized = email.upper()
                        user_id = users_by_email.get(normalized)
                        if user_id is None:
                            user_id = uuid.uuid4()
                            stamp = str(uuid.uuid4())
                            cur.execute(
                                """
                                INSERT INTO users (
                                    "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
                                    "EmailConfirmed", "FullName", "UserKind", "CreatedAt",
                                    "AccessFailedCount", "LockoutEnabled", "PhoneNumberConfirmed",
                                    "TwoFactorEnabled", "SecurityStamp", "ConcurrencyStamp")
                                VALUES (
                                    %s, %s, %s, %s, %s,
                                    TRUE, %s, %s, %s,
                                    0, TRUE, FALSE,
                                    FALSE, %s, %s)
                                """,
                                (
                                    str(user_id),
                                    email,
                                    normalized,
                                    email,
                                    normalized,
                                    full_name,
                                    USER_KIND_EMPLOYEE,
                                    now,
                                    stamp,
                                    stamp,
                                ),
                            )
                            users_by_email[normalized] = user_id
                            stats["users_created"] += 1
                        else:
                            stats["users_existing"] += 1

                        role_key = (user_id, employee_role_id)
                        if role_key not in existing_user_roles:
                            cur.execute(
                                """
                                INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
                                VALUES (%s, %s)
                                ON CONFLICT DO NOTHING
                                """,
                                (str(user_id), str(employee_role_id)),
                            )
                            existing_user_roles.add(role_key)
                            stats["roles_assigned"] += 1

                        membership_key = (department_id, user_id)
                        if membership_key in existing_memberships:
                            stats["dept_employees_existing"] += 1
                            continue

                        cur.execute(
                            """
                            INSERT INTO department_employees (
                                "Id", "DepartmentId", "UserId", "FullName",
                                "IsActive", "CreatedAt")
                            VALUES (%s, %s, %s, %s, TRUE, %s)
                            """,
                            (str(uuid.uuid4()), str(department_id), str(user_id), full_name, now),
                        )
                        existing_memberships.add(membership_key)
                        stats["dept_employees_created"] += 1

            # Backfill defence sessions without department (legacy rows)
            cur.execute(
                """
                UPDATE defence_sessions
                SET "DepartmentId" = sub."Id"
                FROM (
                    SELECT d."Id"
                    FROM departments d
                    WHERE d."IsActive" = TRUE
                      AND lower(d."Name") LIKE %s
                    ORDER BY d."CreatedAt", d."Id"
                    LIMIT 1
                ) sub
                WHERE defence_sessions."DepartmentId" IS NULL
                  AND sub."Id" IS NOT NULL
                """,
                ("%комп%ютерн%систем%",),
            )
            sessions_backfilled = cur.rowcount

            cur.execute(
                """
                UPDATE defence_sessions
                SET "DepartmentId" = sub."Id"
                FROM (
                    SELECT "Id" FROM departments
                    WHERE "IsActive" = TRUE
                    ORDER BY "CreatedAt", "Id"
                    LIMIT 1
                ) sub
                WHERE defence_sessions."DepartmentId" IS NULL
                """,
            )
            sessions_backfilled += cur.rowcount

        conn.commit()

    print("Import completed:")
    for key, value in stats.items():
        print(f"  {key}: {value}")
    print(f"  sessions_backfilled: {sessions_backfilled}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
