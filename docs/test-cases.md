# Test cases catalog (unit)

Повний реєстр unit-тест кейсів. **ID** посилається з коментарів у тестах (`// TC-DOM-001`).

**Status:** `implemented` | `pending` | `integration-only`

Прогрес: [test-progress.md](./test-progress.md)

---

## 1. Domain

### 1.1 CheckpointOutcomeRules

| ID | Case | Input | Expected | Status | Test |
|----|------|-------|----------|--------|------|
| TC-DOM-CHK-001 | Approved is passing | `Approved` | `IsPassing=true` | implemented | `IsPassing_Approved_ReturnsTrue` |
| TC-DOM-CHK-002 | NotApproved is not passing | `NotApproved` | `IsPassing=false` | implemented | `IsPassing_NotApproved_ReturnsFalse` |
| TC-DOM-CHK-003 | NotApproved not passing | `NotApproved` | `IsPassing=false` | implemented | `IsPassing_NotApproved_ReturnsFalse` |
| TC-DOM-CHK-004 | Null outcome not passing | `null` | `IsPassing=false` | implemented | `IsPassing_Null_ReturnsFalse` |
| TC-DOM-CHK-005 | NotApproved requires comment | `NotApproved` | `RequiresComment=true` | implemented | `RequiresComment_NotApproved_ReturnsTrue` |
| TC-DOM-CHK-006 | Approved no comment required | `Approved` | `RequiresComment=false` | implemented | `RequiresComment_Approved_ReturnsFalse` |
| TC-DOM-CHK-007 | Approved no comment required | `Approved` | `RequiresComment=false` | implemented | `RequiresComment_Approved_ReturnsFalse` |

### 1.2 AdmissionStepStatusResolver

| ID | Case | Input | Expected | Status | Test |
|----|------|-------|----------|--------|------|
| TC-DOM-ASR-001 | HasPassingAttempt when approved exists | step + attempts | `true` | implemented | `HasPassingAttempt_WhenApproved_ReturnsTrue` |
| TC-DOM-ASR-002 | HasPassingAttempt when only rejected | step + attempts | `false` | implemented | `HasPassingAttempt_WhenOnlyRejected_ReturnsFalse` |
| TC-DOM-ASR-003 | GetLastAttempt returns highest attempt number | 2 attempts | latest by number | implemented | `GetLastAttempt_ReturnsHighestAttemptNumber` |
| TC-DOM-ASR-004 | GetLastPassingAttempt skips rejected | mixed | last passing | implemented | `GetLastPassingAttempt_SkipsRejected` |
| TC-DOM-ASR-005 | IsStepActionable when no attempts | empty | `true` | implemented | `IsStepActionable_NoAttempts_ReturnsTrue` |
| TC-DOM-ASR-006 | IsStepActionable when last passing | approved attempt | `false` | implemented | `IsStepActionable_LastPassing_ReturnsFalse` |
| TC-DOM-ASR-007 | IsStepActionable reviewer assignment | `ReviewerAssignment` | `false` | implemented | `IsStepActionable_ReviewerAssignment_ReturnsFalse` |
| TC-DOM-ASR-008 | IsStepActionable when last rejected | rejected | `true` | implemented | `IsStepActionable_LastRejected_ReturnsTrue` |
| TC-DOM-ASR-009 | CanSecretaryOverride no current step | `CurrentAdmissionStep=null` | `false` | implemented | `CanSecretaryOverride_NoCurrentStep_ReturnsFalse` |
| TC-DOM-ASR-010 | CanSecretaryOverride reviewer assignment step | `ReviewerAssignment` | `false` | implemented | `CanSecretaryOverride_ReviewerAssignment_ReturnsFalse` |
| TC-DOM-ASR-011 | CanSecretaryOverride external without reviewer | ExternalReview, NotAssigned | `false` | implemented | `CanSecretaryOverride_ExternalWithoutReviewer_ReturnsFalse` |
| TC-DOM-ASR-012 | CanSecretaryOverride actionable step | formatting, prior passing | `true` | implemented | `CanSecretaryOverride_ActionableStep_ReturnsTrue` |
| TC-DOM-ASR-013 | AreAllOutcomeStepsPassing all pass | 4 passing | `true` | implemented | `AreAllOutcomeStepsPassing_AllPass_ReturnsTrue` |
| TC-DOM-ASR-014 | AreAllOutcomeStepsPassing missing step | 3 of 4 | `false` | implemented | `AreAllOutcomeStepsPassing_MissingStep_ReturnsFalse` |
| TC-DOM-ASR-015 | ResolveCurrentStep first step | no attempts | `SupervisorFeedback` | implemented | `ResolveCurrentStep_NoAttempts_ReturnsSupervisorFeedback` |
| TC-DOM-ASR-015a | ResolveCurrentStep supervisor rejected | rejected attempt | `SupervisorFeedback` | implemented | `ResolveCurrentStep_SupervisorRejected_ReturnsSupervisorFeedback` |
| TC-DOM-ASR-015b | ResolveCurrentStep formatting rejected | supervisor pass, formatting fail | `FormattingReview` | implemented | `ResolveCurrentStep_FormattingNotPassing_ReturnsFormattingReview` |
| TC-DOM-ASR-016 | ResolveCurrentStep reviewer assignment | 3 passing | `ReviewerAssignment` | implemented | `ResolveCurrentStep_ReadyForReviewer_ReturnsReviewerAssignment` |
| TC-DOM-ASR-017 | ResolveCurrentStep external review | reviewer assigned | `ExternalReview` | implemented | `ResolveCurrentStep_ReviewerAssigned_ReturnsExternalReview` |
| TC-DOM-ASR-017a | ResolveCurrentStep completed, no external | `Completed`, 3 checkpoints | `ExternalReview` | implemented | `ResolveCurrentStep_CompletedWithoutExternalReview_ReturnsExternalReview` |
| TC-DOM-ASR-018 | ResolveCurrentStep all complete | all passing + review done | `null` | implemented | `ResolveCurrentStep_AllComplete_ReturnsNull` |

### 1.3 SupervisorOverridePolicy

| ID | Case | Input | Expected | Status | Test |
|----|------|-------|----------|--------|------|
| TC-DOM-SOP-001 | AllowsLifecycle before work in progress | `AwaitingSupervisor` | `true` | implemented | `AllowsLifecycle_BeforeWorkInProgress_ReturnsTrue` |
| TC-DOM-SOP-002 | AllowsLifecycle after reviewer assigned | `ReviewerAssigned` | `false` | implemented | `AllowsLifecycle_AfterTopicApproved_ReturnsFalse` |
| TC-DOM-SOP-003 | AllowsAdmission when not admitted | `NotAdmitted` | `true` | implemented | `AllowsAdmission_NotAdmitted_ReturnsTrue` |
| TC-DOM-SOP-004 | AllowsAdmission when admitted | `Admitted` | `false` | implemented | `AllowsAdmission_Admitted_ReturnsFalse` |
| TC-DOM-SOP-005 | EnsureCanOverride archived session | archived | `DomainException` session archived | implemented | `EnsureCanOverride_ArchivedSession_Throws` |
| TC-DOM-SOP-006 | EnsureCanOverride already admitted | admitted | `DomainException` already admitted | implemented | `EnsureCanOverride_AlreadyAdmitted_Throws` |
| TC-DOM-SOP-007 | EnsureCanOverride topic approved lifecycle | work in progress | `DomainException` topic approved | implemented | `EnsureCanOverride_TopicApprovedLifecycle_Throws` |
| TC-DOM-SOP-008 | EnsureCanOverride valid | awaiting supervisor, active | no throw | implemented | `EnsureCanOverride_Valid_DoesNotThrow` |

### 1.4 SecretarySupervisorOverrideService

| ID | Case | Input | Expected | Status | Test |
|----|------|-------|----------|--------|------|
| TC-DOM-SSO-001 | Override sets supervisor confirmed | valid diploma | `SupervisorId`, `Confirmed`, `UpdatedAt` | implemented | `Override_SetsSupervisorAndConfirmed` |
| TC-DOM-SSO-002 | Override policy violation | admitted | throws | implemented | `Override_WhenAdmitted_Throws` |

### 1.5 DiplomaLifecycleService

| ID | Case | Input | Expected | Status | Test |
|----|------|-------|----------|--------|------|
| TC-DOM-DLS-001 | Admitted status wins | `AdmissionStatus=Admitted` | `Admitted` lifecycle | implemented | `Recalculate_Admitted_ReturnsAdmitted` |
| TC-DOM-DLS-002 | Ready for admission | all checks pass | `ReadyForAdmission` | implemented | `Recalculate_ReadyForAdmission_ReturnsReady` |
| TC-DOM-DLS-003 | Documents in progress | attempts exist | `DocumentsInProgress` | implemented | `Recalculate_WithAttempts_ReturnsDocumentsInProgress` |
| TC-DOM-DLS-004 | Topic approved without reviewer | approved topic, no reviewer | `TopicApproved` | implemented | `Recalculate_ApprovedTopicWithoutReviewer_ReturnsTopicApproved` |
| TC-DOM-DLS-005 | Topic in review | pending topic | `TopicInReview` | implemented | `Recalculate_PendingTopic_ReturnsTopicInReview` |
| TC-DOM-DLS-006 | Supervisor confirmed | confirmed, no topic | `SupervisorConfirmed` | implemented | `Recalculate_SupervisorConfirmed_ReturnsSupervisorConfirmed` |
| TC-DOM-DLS-007 | Awaiting supervisor | default | `AwaitingSupervisor` | implemented | `Recalculate_Default_ReturnsAwaitingSupervisor` |
| TC-DOM-DLS-008 | CanStartAdmissionReview valid | approved topic, work in progress | `true` | implemented | `CanStartAdmissionReview_Valid_ReturnsTrue` |
| TC-DOM-DLS-009 | CanStartAdmissionReview with attempts | attempts > 0 | `false` | implemented | `CanStartAdmissionReview_WithAttempts_ReturnsFalse` |

### 1.6 DiplomaCreationService

| ID | Case | Input | Expected | Status | Test |
|----|------|-------|----------|--------|------|
| TC-DOM-DCR-001 | Creates diploma for new student | 1 candidate | 1 diploma, correct defaults | implemented | `CreateForStudents_NewStudent_CreatesDiploma` |
| TC-DOM-DCR-002 | Skips existing student in session | duplicate id | empty list | implemented | `CreateForStudents_ExistingStudent_Skips` |
| TC-DOM-DCR-003 | Non-student user kind | employee kind | `DomainException` | implemented | `CreateForStudents_NonStudent_Throws` |
| TC-DOM-DCR-004 | Multiple new students | 2 candidates | 2 diplomas | implemented | `CreateForStudents_Multiple_CreatesAll` |

### 1.7 Existing domain services (pre-catalog)

Деталі в іменах тестів у `tests/DiplomaManagementSystem.Domain.Tests/`. Статус: **implemented** (89 тестів, хвилі 1–16).

### 1.8 Entity model smoke (хвиля 16)

| ID | Case | Expected | Status | Test |
|----|------|----------|--------|------|
| TC-DOM-ENT-001 | Diploma navigation collections | set/get session, documents, comments, attempts, topics | implemented | `Diploma_CanSetNavigationProperties` |
| TC-DOM-ENT-002 | StudyGroup → DefenceSession | navigation round-trip | implemented | `StudyGroup_CanSetDefenceSessionNavigation` |
| TC-DOM-ENT-003 | AuditLog → DefenceSession | navigation round-trip | implemented | `AuditLog_CanSetDefenceSessionNavigation` |
| TC-DOM-ENT-004 | DiplomaDocument → AdmissionStepAttempt | navigation round-trip | implemented | `DiplomaDocument_CanSetAdmissionStepAttemptNavigation` |

---

## 2. Application — Authorization

### 2.1 DiplomaAuthorizationService — prerequisites

| ID | Case | Input | Expected | Status | Test |
|----|------|-------|----------|--------|------|
| TC-APP-AUTH-001 | Diploma not found | unknown id | `DomainException` DiplomaNotFound | implemented | `EnsureCanPerform_DiplomaNotFound_Throws` |
| TC-APP-AUTH-002 | Session mismatch | wrong expectedSessionId | `DomainException` SessionMismatch | implemented | `EnsureCanPerform_SessionMismatch_Throws` |
| TC-APP-AUTH-003 | Archived session | archived defence session | `DomainException` SessionArchived | implemented | `EnsureCanPerform_ArchivedSession_Throws` |
| TC-APP-AUTH-004 | Topic version not found | unknown versionId | `DomainException` TopicVersionNotFound | implemented | `EnsureCanPerformOnTopicVersion_NotFound_Throws` |
| TC-APP-AUTH-005 | Invalid enum action | cast invalid | `DomainException` UnsupportedAction | implemented | `EnsureCanPerform_UnsupportedAction_Throws` |

### 2.2 Supervisor actions

| ID | Case | Input | Expected | Status | Test |
|----|------|-------|----------|--------|------|
| TC-APP-AUTH-010 | ConfirmSupervisor — assigned supervisor | matching userId | success | implemented | `SupervisorAction_AssignedSupervisor_Succeeds` |
| TC-APP-AUTH-011 | ConfirmSupervisor — wrong user | other userId | NotSupervisor | implemented | `SupervisorAction_WrongUser_Throws` |
| TC-APP-AUTH-012 | RejectSupervisor — same rules | | same as confirm | implemented | `RejectSupervisor_WrongUser_Throws` |
| TC-APP-AUTH-013 | CompleteSupervisorCheckpoint — supervisor | matching | success | implemented | `CompleteSupervisorCheckpoint_Supervisor_Succeeds` |

### 2.3 Reviewer

| ID | Case | Input | Expected | Status | Test |
|----|------|-------|----------|--------|------|
| TC-APP-AUTH-020 | CompleteExternalReview — assigned reviewer | matching | success | implemented | `CompleteExternalReview_AssignedReviewer_Succeeds` |
| TC-APP-AUTH-021 | CompleteExternalReview — wrong user | other | NotReviewer | implemented | `CompleteExternalReview_WrongUser_Throws` |

### 2.4 Annual roles

| ID | Case | Input | Expected | Status | Test |
|----|------|-------|----------|--------|------|
| TC-APP-AUTH-030 | CompleteAntiPlagiarism — officer | has role | success | implemented | `CompleteAntiPlagiarism_WithRole_Succeeds` |
| TC-APP-AUTH-031 | CompleteAntiPlagiarism — no role | | MissingSessionRole | implemented | `CompleteAntiPlagiarism_NoRole_Throws` |
| TC-APP-AUTH-032 | CompleteFormattingReview — reviewer | has role | success | implemented | `CompleteFormattingReview_WithRole_Succeeds` |
| TC-APP-AUTH-033 | CompleteFormattingReview — no role | | MissingSessionRole | implemented | `CompleteFormattingReview_NoRole_Throws` |
| TC-APP-AUTH-034 | ApproveTopicAsDepartmentHead — head | has role | success | implemented | `DepartmentHeadTopicAction_WithRole_Succeeds` |
| TC-APP-AUTH-035 | ApproveTopicAsDepartmentHead — no role | | NotDepartmentHead | implemented | `DepartmentHeadTopicAction_NoRole_Throws` |
| TC-APP-AUTH-036 | RejectTopicAsDepartmentHead — via version | head on version | success | implemented | `RejectTopicAsDepartmentHead_OnVersion_Succeeds` |

### 2.5 Secretary

| ID | Case | Input | Expected | Status | Test |
|----|------|-------|----------|--------|------|
| TC-APP-AUTH-040 | AssignReviewer — secretary | can access session | success | implemented | `SecretaryAction_WithAccess_Succeeds` |
| TC-APP-AUTH-041 | AssignReviewer — not secretary | | NotSecretaryForSession | implemented | `SecretaryAction_NoAccess_Throws` |
| TC-APP-AUTH-042 | AdmitDiploma — secretary | | success | implemented | `AdmitDiploma_WithAccess_Succeeds` |
| TC-APP-AUTH-043 | OverrideSupervisor — secretary | | success | implemented | `OverrideSupervisor_WithAccess_Succeeds` |
| TC-APP-AUTH-044 | AddSecretaryComment — secretary | | success | implemented | `AddSecretaryComment_WithAccess_Succeeds` |
| TC-APP-AUTH-045 | OverrideAdmissionStep — secretary | | success | implemented | `OverrideAdmissionStep_WithAccess_Succeeds` |

### 2.6 Topic version — supervisor

| ID | Case | Input | Expected | Status | Test |
|----|------|-------|----------|--------|------|
| TC-APP-AUTH-050 | ApproveTopicAsSupervisor on version | supervisor | success | implemented | `ApproveTopicAsSupervisor_OnVersion_Succeeds` |
| TC-APP-AUTH-051 | ApproveTopicAsSupervisor wrong user | | NotSupervisor | implemented | `ApproveTopicAsSupervisor_WrongUser_Throws` |
| TC-APP-AUTH-052 | RejectTopicAsSupervisor on version | supervisor | success | implemented | `RejectTopicAsSupervisor_OnVersion_Succeeds` |

---

## 3. Application — DiplomaWorkflowGuidance

| ID | Case | Condition | Expected | Status | Test |
|----|------|-----------|----------|--------|------|
| TC-APP-GUI-001 | AssignReviewer hidden section | `showSection=false` | `null` | implemented | `AssignReviewer_HiddenSection_ReturnsNull` |
| TC-APP-GUI-002 | AssignReviewer no employees | | admin message | implemented | `AssignReviewer_NoEmployees_ReturnsMessage` |
| TC-APP-GUI-003 | AssignReviewer no topic | | student no topic | implemented | `AssignReviewer_NoTopic_ReturnsMessage` |
| TC-APP-GUI-004 | AssignReviewer topic approved | | `null` | implemented | `AssignReviewer_TopicApproved_ReturnsNull` |
| TC-APP-GUI-005 | AssignReviewer already assigned | | already assigned msg | implemented | `AssignReviewer_AlreadyAssigned_ReturnsMessage` |
| TC-APP-GUI-006 | AssignReviewer review completed | | completed msg | implemented | `AssignReviewer_ReviewCompleted_ReturnsMessage` |
| TC-APP-GUI-007 | DeclareWorkReady archived | | archived msg | implemented | `DeclareWorkReady_Archived_ReturnsMessage` |
| TC-APP-GUI-008 | DeclareWorkReady wrong lifecycle | | checks started | implemented | `DeclareWorkReady_WrongLifecycle_ReturnsMessage` |
| TC-APP-GUI-009 | DeclareWorkReady no work uploaded | | upload first | implemented | `DeclareWorkReady_NoWork_ReturnsMessage` |
| TC-APP-GUI-010 | DeclareWorkReady valid | | `null` | implemented | `DeclareWorkReady_Valid_ReturnsNull` |
| TC-APP-GUI-011 | UploadWork archived | | archived upload | implemented | `UploadWork_Archived_ReturnsMessage` |
| TC-APP-GUI-012 | UploadWork no approved topic | | after topic approved | implemented | `UploadWork_NoTopic_ReturnsMessage` |
| TC-APP-GUI-013 | UploadWork admitted | | after admitted | implemented | `UploadWork_Admitted_ReturnsMessage` |
| TC-APP-GUI-014 | UploadWork wrong lifecycle | awaiting supervisor | wrong lifecycle | implemented | `UploadWork_WrongLifecycle_ReturnsMessage` |
| TC-APP-GUI-015 | UploadWork valid | work in progress | `null` | implemented | `UploadWork_Valid_ReturnsNull` |
| TC-APP-GUI-016 | OverrideSupervisor archived | | archived short | implemented | `OverrideSupervisor_Archived_ReturnsMessage` |
| TC-APP-GUI-017 | OverrideSupervisor after topic | work in progress | change after topic | implemented | `OverrideSupervisor_AfterTopic_ReturnsMessage` |
| TC-APP-GUI-018 | OverrideSupervisor valid | awaiting | `null` | implemented | `OverrideSupervisor_Valid_ReturnsNull` |
| TC-APP-GUI-019 | Admit ready | ReadyForAdmission | `null` | implemented | `Admit_Ready_ReturnsNull` |
| TC-APP-GUI-020 | Admit not ready | missing blockers | lists blockers | implemented | `Admit_NotReady_ListsBlockers` |
| TC-APP-GUI-021 | Admit archived | | archived admit | implemented | `Admit_Archived_ReturnsMessage` |
| TC-APP-GUI-022 | OverrideAdmission before work ready | no step | work ready hint | implemented | `OverrideAdmission_BeforeWorkReady_ReturnsMessage` |
| TC-APP-GUI-023 | OverrideAdmission step completed | passing attempt | completed hint | implemented | `OverrideAdmission_StepCompleted_ReturnsMessage` |
| TC-APP-GUI-024 | OverrideAdmission external no reviewer | | reviewer not assigned | implemented | `OverrideAdmission_ExternalNoReviewer_ReturnsMessage` |
| TC-APP-GUI-025 | OverrideAdmission valid | actionable | `null` | implemented | `OverrideAdmission_Valid_ReturnsNull` |
| TC-APP-GUI-026 | AddComment admitted | | after admitted | implemented | `AddComment_Admitted_ReturnsMessage` |
| TC-APP-GUI-027 | AddComment archived | not admitted | archived comments | implemented | `AddComment_Archived_ReturnsMessage` |
| TC-APP-GUI-028 | AddComment valid | | `null` | implemented | `AddComment_Valid_ReturnsNull` |
| TC-APP-GUI-029 | SelectSupervisor pending | supervisorId set | pending msg | implemented | `SelectSupervisor_Pending_ReturnsMessage` |
| TC-APP-GUI-030 | SelectSupervisor valid | no supervisorId | `null` | implemented | `SelectSupervisor_Valid_ReturnsNull` |
| TC-APP-GUI-031 | SelectSupervisor confirmed | | confirmed msg | implemented | `SelectSupervisor_Confirmed_ReturnsMessage` |
| TC-APP-GUI-032 | SelectSupervisor no employees | | no employees student | implemented | `SelectSupervisor_NoEmployees_ReturnsMessage` |
| TC-APP-GUI-033 | SubmitTopic archived | | archived topic | implemented | `SubmitTopic_Archived_ReturnsMessage` |
| TC-APP-GUI-034 | SubmitTopic supervisor not confirmed | | select supervisor | implemented | `SubmitTopic_NoSupervisor_ReturnsMessage` |
| TC-APP-GUI-035 | SubmitTopic pending supervisor | | await supervisor | implemented | `SubmitTopic_PendingSupervisor_ReturnsMessage` |
| TC-APP-GUI-036 | SubmitTopic already approved | | already approved | implemented | `SubmitTopic_AlreadyApproved_ReturnsMessage` |
| TC-APP-GUI-037 | SubmitTopic pending head | | pending head | implemented | `SubmitTopic_PendingHead_ReturnsMessage` |
| TC-APP-GUI-038 | SubmitTopic rejected resubmit | | rejected resubmit | implemented | `SubmitTopic_Rejected_ReturnsMessage` |
| TC-APP-GUI-039 | SubmitTopic valid | confirmed supervisor | `null` | implemented | `SubmitTopic_Valid_ReturnsNull` |
| TC-APP-GUI-040 | AssignReviewer pending supervisor topic | latest pending supervisor | long await msg | implemented | `AssignReviewer_PendingSupervisorTopic_ReturnsMessage` |
| TC-APP-GUI-041 | AssignReviewer pending head topic | | await head long | implemented | `AssignReviewer_PendingHeadTopic_ReturnsMessage` |
| TC-APP-GUI-042 | AssignReviewer rejected topic | | rejected student | implemented | `AssignReviewer_RejectedTopic_ReturnsMessage` |

---

## 4. Application — StudentWorkflowProgressBuilder

| ID | Case | State | Expected | Status | Test |
|----|------|-------|----------|--------|------|
| TC-APP-SWP-001 | Awaiting supervisor | default | step 0 current | implemented | `Build_WhenAwaitingSupervisor_CurrentStepIsSupervisor` |
| TC-APP-SWP-002 | Pending supervisor request | supervisorId set | waiting hint | implemented | `Build_WhenSupervisorRequestPending_ShowsWaitingHint` |
| TC-APP-SWP-003 | Supervisor confirmed | | step 1 current | implemented | `Build_WhenSupervisorConfirmed_CurrentStepIsTopic` |
| TC-APP-SWP-004 | Admitted | all complete | 8 steps, 100% | implemented | `Build_WhenAdmitted_AllStepsCompleted` |
| TC-APP-SWP-005 | Secretary assign reviewer | reviewer assignment step | assign hint | implemented | `Build_ForSecretaryAtReviewStep_SuggestsAssignReviewer` |
| TC-APP-SWP-006 | Archived session | sessionActive=false | archived hints | implemented | `Build_WhenSessionArchived_AppendsArchivedSuffixToHint` |
| TC-APP-SWP-007 | Topic in review | pending topic | topic step current | implemented | `Build_WhenTopicInReview_CurrentStepIsTopicSubmission` |
| TC-APP-SWP-008 | Documents in progress student | checkpoints started | checkpoint step | implemented | `Build_WhenDocumentsInProgress_CurrentStepIsSupervisorFeedback` |
| TC-APP-SWP-009 | Ready for admission | | admit step hint | implemented | `Build_WhenReadyForAdmission_ShowsAdmitHint` |
| TC-APP-SWP-010 | Secretary pending supervisor | | waiting hint | implemented | `Build_ForSecretaryWithPendingSupervisor_ShowsWaitingHint` |
| TC-APP-SWP-011 | Topic approved step detail | approved version | detail lines | implemented | `Build_WhenTopicApproved_IncludesApprovalDetailOnStepThree` |
| TC-APP-SWP-012 | Checkpoint status rejected | rejected attempt | rejected detail | implemented | `Build_WhenCheckpointRejected_ShowsRejectedBadge` |
| TC-APP-SWP-013 | Checkpoint passing | approved attempt | completed badge + recorder | implemented | `Build_WhenCheckpointPassing_ShowsCompletedBadgeAndRecorder` |
| TC-APP-SWP-014 | Checkpoint locked | prior steps incomplete | waiting prior badge | implemented | `Build_WhenCheckpointLocked_ShowsWaitingForPriorBadge` |
| TC-APP-SWP-015 | External review current | reviewer assigned | current badge | implemented | `Build_WhenAssignedReviewer_CurrentExternalReviewBadge` |
| TC-APP-SWP-016 | Secretary override attempt | override flag | `IsSecretaryOverride` | implemented | `Build_WhenSecretaryOverrideAttempt_FlagsStatus` |
| TC-APP-SWP-017 | Admitted secretary hint | defence date | secretary message | implemented | `Build_WhenAdmittedForSecretary_IncludesDefenceDateInHint` |
| TC-APP-SWP-018 | Supervisor detail | name + status | detail line | implemented | `Build_WhenSupervisorConfirmed_ShowsSupervisorNameInDetail` |
| TC-APP-SWP-019 | Reviewer detail | reviewer name | detail line | implemented | `Build_WhenReviewerAssigned_ShowsReviewerNameInDetail` |

---

## 5. Application — Helpers

| ID | Component | Case | Expected | Status | Test |
|----|-----------|------|----------|--------|------|
| TC-APP-HLP-001 | ArchiveGuard | active session | no throw | implemented | `EnsureWritable_Active_DoesNotThrow` |
| TC-APP-HLP-002 | ArchiveGuard | archived | DomainException | implemented | `EnsureWritable_Archived_Throws` |
| TC-APP-HLP-003 | DiplomaStoragePathBuilder | full segments | year/session/group/student | implemented | `BuildFolderSegments_ReturnsExpectedSegments` |
| TC-APP-HLP-004 | DiplomaStoragePathBuilder | sanitize invalid chars | `_` replacement | implemented | `SanitizeSegment_ReplacesInvalidCharacters` |
| TC-APP-HLP-005 | DiplomaStoragePathBuilder | empty segment | `unknown` | implemented | `SanitizeSegment_Empty_ReturnsUnknown` |
| TC-APP-HLP-006 | SecretarySessionLabel | bachelor + semester | formatted UA | implemented | `Format_BachelorWithSemester_ReturnsLabel` |
| TC-APP-HLP-007 | SecretarySessionLabel | master no semester | formatted | implemented | `Format_MasterNoSemester_ReturnsLabel` |
| TC-APP-HLP-008 | WorkflowUkrainianLabels | admission steps | UA labels | implemented | `FormatAdmissionStep_ReturnsUkrainian` |
| TC-APP-HLP-009 | WorkflowUkrainianLabels | checkpoint outcomes | UA labels | implemented | `FormatCheckpointOutcome_ReturnsUkrainian` |
| TC-APP-HLP-010 | WorkflowUkrainianLabels | override comment prefix | contains step name | implemented | `BuildAdmissionStepOverrideCommentPrefix_ContainsStep` |
| TC-APP-HLP-011 | TopicVersionApprovalFormatter | approved version | display lines | implemented | `BuildApprovedDisplay_ReturnsLines` |
| TC-APP-HLP-012 | TopicVersionApprovalFormatter | non-approved | `null` | implemented | `BuildApprovedDisplay_NotApproved_ReturnsNull` |
| TC-APP-HLP-013 | TopicVersionApprovalFormatter | rejection line | formatted | implemented | `FormatRejectionLine_ReturnsLine` |
| TC-APP-HLP-014 | TopicVersionApprovalFormatter | head pending | await head | implemented | `FormatHeadLine_PendingHead_ReturnsMessage` |
| TC-APP-HLP-015 | TopicVersionApprovalFormatter | step detail | multiline | implemented | `BuildTopicApprovedStepDetail_IncludesLines` |
| TC-APP-HLP-016 | PersonNameSort | empty name | empty key | implemented | `SurnameKey_WhenMissing_ReturnsEmpty` |
| TC-APP-HLP-017 | DefenceWorkLabel | plural/accusative/instrumental | UA labels | implemented | `Plural_ReturnsExpectedLabel` etc. |
| TC-APP-HLP-018 | EmailDomainValidator | malformed email | false | implemented | `IsAllowed_WhenEmailMalformed_ReturnsFalse` |
| TC-APP-HLP-019 | ImportRowProcessor | validation/domain/generic errors | skip + message | implemented | `ImportRowProcessorTests` |
| TC-APP-HLP-020 | DiplomaDocumentNaming | requires file / throws | mapping rules | implemented | `DiplomaDocumentNamingTests` |

### 5.1 List projections (хвиля 17)

| ID | Component | Case | Expected | Status | Test |
|----|-----------|------|----------|--------|------|
| TC-APP-PRJ-001 | SecretaryDiplomaListProjection | missing student | placeholders | implemented | `MapListItem_WhenStudentMissing_UsesPlaceholderLabels` |
| TC-APP-PRJ-002 | SecretaryDiplomaListProjection | full mapping | supervisor/topic/steps | implemented | `MapListItem_MapsSupervisorTopicAndCompletedSteps` |
| TC-APP-PRJ-003 | SecretaryDiplomaListProjection | sort | surname order | implemented | `MapListItems_SortsBySurnameThenFullName` |
| TC-APP-PRJ-004 | EmployeeDiplomaListProjection | pending students | placeholders + sort | implemented | `MapPendingStudentsAsync_WhenDisplayMissing_UsesPlaceholderAndSortsBySurname` |
| TC-APP-PRJ-005 | EmployeeDiplomaListProjection | checkpoint items | topic title | implemented | `MapPendingCheckpointItemsAsync_MapsTopicTitle` |
| TC-APP-PRJ-006 | EmployeeDiplomaListProjection | reviewer assignments | status + topic | implemented | `MapReviewerAssignmentsAsync_MapsStatusAndTopic` |

---

## 6. Application — FluentValidation validators

### Student

| ID | Validator | Case | Expected | Status | Test |
|----|-----------|------|----------|--------|------|
| TC-APP-VAL-001 | SelectSupervisor | empty diplomaId | invalid | implemented | `SelectSupervisor_EmptyDiplomaId_Invalid` |
| TC-APP-VAL-002 | SelectSupervisor | empty supervisorId | invalid | implemented | `SelectSupervisor_EmptySupervisorId_Invalid` |
| TC-APP-VAL-003 | SelectSupervisor | valid | valid | implemented | `SelectSupervisor_Valid_Valid` |
| TC-APP-VAL-004 | SubmitTopic | empty title | invalid | implemented | `SubmitTopic_EmptyTitle_Invalid` |
| TC-APP-VAL-005 | SubmitTopic | title too long | invalid | implemented | `SubmitTopic_TitleTooLong_Invalid` |
| TC-APP-VAL-006 | SubmitTopic | valid | valid | implemented | `SubmitTopic_Valid_Valid` |

### Secretary

| ID | Validator | Case | Expected | Status | Test |
|----|-----------|------|----------|--------|------|
| TC-APP-VAL-010 | AssignReviewer | empty ids | invalid | implemented | `AssignReviewer_EmptyIds_Invalid` |
| TC-APP-VAL-011 | AdmitDiploma | empty defence date | invalid | implemented | `AdmitDiploma_EmptyDefenceDate_Invalid` |
| TC-APP-VAL-012 | OverrideSupervisor | empty reason | invalid | implemented | `OverrideSupervisor_EmptyReason_Invalid` |
| TC-APP-VAL-013 | AddComment | empty body | invalid | implemented | `AddComment_EmptyBody_Invalid` |
| TC-APP-VAL-014 | OverrideAdmissionStep | empty comment | invalid | implemented | `OverrideAdmissionStep_EmptyComment_Invalid` |
| TC-APP-VAL-015 | Secretary validators | valid DTOs | valid | implemented | `SecretaryValidators_Valid_Valid` |

### Employee

| ID | Validator | Case | Expected | Status | Test |
|----|-----------|------|----------|--------|------|
| TC-APP-VAL-020 | CompleteCheckpoint | NotApproved no comment | invalid | implemented | `CompleteCheckpoint_NotApprovedWithoutComment_Invalid` |
| TC-APP-VAL-021 | CompleteCheckpoint | NotApproved with comment | valid | implemented | `CompleteCheckpoint_NotApprovedWithComment_Valid` |
| TC-APP-VAL-022 | CompleteCheckpoint | Approved no comment | valid | implemented | `CompleteCheckpoint_ApprovedWithoutComment_Valid` |

### Import

| ID | Validator | Case | Expected | Status | Test |
|----|-----------|------|----------|--------|------|
| TC-APP-VAL-030 | EmployeeImportRow | empty name | invalid | implemented | `EmployeeImportRow_EmptyName_Invalid` |
| TC-APP-VAL-031 | EmployeeImportRow | invalid email | invalid | implemented | `EmployeeImportRow_InvalidEmail_Invalid` |
| TC-APP-VAL-032 | EmployeeImportRow | valid | valid | implemented | `EmployeeImportRow_Valid_Valid` |
| TC-APP-VAL-033 | StudentImportRow | | | implemented | existing tests |

---

## 7. Application — Admin (existing + pending)

| ID | Component | Case | Status | Test |
|----|-----------|------|--------|------|
| TC-APP-ADM-001 | EmployeeAdminService | CRUD | implemented | existing |
| TC-APP-ADM-002 | StudentAdminService | create | implemented | existing |
| TC-APP-ADM-003 | StudyGroupAdminService | create/list | implemented | existing |
| TC-APP-ADM-004 | AnnualRoleService | assign / update / errors | implemented | `AnnualRoleServiceTests` |
| TC-APP-ADM-005 | Admin form validators | year, semester, email domain | implemented | `AdminFormValidatorTests` |

### Import — ImportResultComposer

| ID | Case | Expected | Status | Test |
|----|------|----------|--------|------|
| TC-APP-IMP-001 | Merge parse + processed errors | combined counts | implemented | `Combine_MergesParseErrorsAndCounts` |
| TC-APP-IMP-002 | No parse errors | processed only | implemented | `Combine_WhenNoParseErrors_ReturnsProcessedCounts` |
| TC-APP-IMP-003 | Only parse errors | all skipped | implemented | `Combine_WhenOnlyParseErrors_SkipsAllRows` |

---

## 8. Web (unit)

| ID | Mapper | Case | Status | Test |
|----|--------|------|--------|------|
| TC-WEB-MAP-001 | SecretaryDiplomaDetailsMapper | header, history, actions, workflow, documents | implemented | `SecretaryDiplomaDetailsMapperTests` |
| TC-WEB-MAP-002 | StudentDiplomaViewModelMapper | composite sections + empty diploma | implemented | `StudentDiplomaViewModelMapperTests` |
| TC-WEB-MAP-003 | SecretaryListViewModelMapper | list item + filter selects | implemented | `SecretaryListAndDashboardMapperTests` |
| TC-WEB-MAP-004 | SecretaryDashboardViewModelMapper | buckets + labels | implemented | `MapDashboard_MapsBucketsWithLabels` |
| TC-WEB-MAP-005 | SecretaryReportsViewModelMapper | admitted items | implemented | `MapReport_MapsAdmittedItems` |
| TC-WEB-MAP-006 | WorkflowProgressMapper | steps, css, metadata | implemented | `WorkflowAndDocumentMapperTests` |
| TC-WEB-MAP-007 | DiplomaDocumentMapper | null + all kinds | implemented | `WorkflowAndDocumentMapperTests` |
| TC-WEB-MAP-008 | TopicHistoryMapper | item + detail overloads | implemented | `WorkflowAndDocumentMapperTests` |
| TC-WEB-MAP-009 | EmployeeViewModelMapper | all map methods | implemented | `EmployeeAndAdminMapperTests` |
| TC-WEB-MAP-010 | AdminDefenceSessionViewModelMapper | list + details | implemented | `EmployeeAndAdminMapperTests` |
| TC-WEB-MAP-011 | UploadFileMapper | map + empty file | implemented | `UploadAndCheckpointHelperTests` |
| TC-WEB-MAP-012 | CheckpointCompletionHelper | required doc rules | implemented | `UploadAndCheckpointHelperTests` |
| TC-WEB-MAP-013 | AdminFlashMessages | Success/Error/Info temp data | implemented | `AdminFlashMessagesTests` |
| TC-WEB-MAP-003 | UkrainianDisplay | enums | implemented | `DisplayLocalizationTests` |
| TC-WEB-MAP-004 | AdminPreviewService | | implemented | `AdminPreviewServiceTests` |

---

## 9. Integration-only cases

Див. [test-plan.md](./test-plan.md) §14. Не дублюються як unit; ID префікс `TC-INT-`.

| ID | Сценарій | Expected | Status | Test |
|----|----------|----------|--------|------|
| TC-INT-001 | Topic approved by head | lifecycle WorkInProgress, approved topic | implemented | `TopicApprovalScenarioTests` |
| TC-INT-002 | Full admission flow | Admitted | implemented | `FullAdmissionScenarioTests` |
| TC-INT-003 | Topic rejected by supervisor | student cannot declare work ready | implemented | `TopicRejectionScenarioTests` |
| TC-INT-004 | Checkpoint rejected → retry | 2 attempts on SupervisorFeedback, advance | implemented | `CheckpointRejectionScenarioTests` |
| TC-INT-005 | Assign reviewer without approved topic | DomainException | implemented | `AuthorizationScenarioTests` |
| TC-INT-006 | Wrong role on checkpoint | NotSupervisor | implemented | `AuthorizationScenarioTests` |
| TC-INT-007 | Secretary override supervisor | audit log OverrideSupervisor | implemented | `SecretaryOverrideAuditScenarioTests` |
| TC-INT-008 | GetMyDiploma composite DTO | header, assignments, state, history | implemented | `MyDiplomaReadScenarioTests` |
| TC-INT-009 | Archived session write block | DomainException | implemented | `ArchivedSessionScenarioTests` |
| TC-INT-010 | Student enrollment | diploma created | implemented | `StudentEnrollmentScenarioTests` |
| TC-INT-011 | Import scenarios | partial / full | implemented | `ImportScenarioTests` |
| TC-INT-012 | Study group duplicate | DomainException | implemented | `StudyGroupAdminScenarioTests` |
| TC-INT-013 | Health endpoint | 200 | implemented | `HealthEndpointTests` |
| TC-INT-014 | Import partial failure | valid rows imported, errors reported | implemented | `ImportScenarioTests.StudentImport_PartialFailure_ImportsValidRowsOnly` |
| TC-INT-015 | Import duplicate email | second row skipped | implemented | `ImportScenarioTests.StudentImport_DuplicateEmail_SkipsSecondRow` |
| TC-INT-016 | Upload invalid file type | DomainException (формати) | implemented | `DocumentUploadScenarioTests` |
| TC-INT-017 | Unauthenticated student area | redirect to login | implemented | `AreaAuthorizationEndpointTests` |
| TC-INT-018 | Student cannot access secretary | redirect AccessDenied | implemented | `AreaAuthorizationEndpointTests` |
| TC-INT-019 | POST select supervisor | redirect + pending assignment | implemented | `StudentSelectSupervisorEndpointTests` |
| TC-INT-020 | Secretary list lifecycle filter | matching diploma | implemented | `SecretaryDiplomaListScenarioTests` |
| TC-INT-021 | Secretary list search | by student name | implemented | `SecretaryDiplomaListScenarioTests` |
| TC-INT-022 | Secretary dashboard buckets | total ≥ 1 | implemented | `SecretaryDashboardScenarioTests` |
| TC-INT-023 | POST secretary admit | redirect + Admitted | implemented | `SecretaryAdmitEndpointTests` |
| TC-INT-024 | Admin preview Set Student | redirect SelectUser | implemented | `AdminPreviewEndpointTests` |
| TC-INT-025 | GetMyDiploma без диплома | empty composite | implemented | `MyDiplomaReadScenarioTests.GetMyDiploma_WithoutDiploma_ReturnsEmptyComposite` |
| TC-INT-026 | Document linked to attempt | AdmissionStepAttemptId set | implemented | `DocumentAttemptLinkScenarioTests` |
| TC-INT-027 | Override admission step | audit + lifecycle advance | implemented | `SecretaryOverrideAdmissionStepScenarioTests` |
| TC-INT-028 | Admitted report | item + CSV export | implemented | `AdmittedReportScenarioTests` |
| TC-INT-029 | Head reject topic | topic Rejected | implemented | `DepartmentHeadTopicRejectionScenarioTests` |
| TC-INT-030 | Supervisor reject student | CanSelectSupervisor | implemented | `SupervisorRejectStudentScenarioTests` |
| TC-INT-031 | Employee home supervisor | pending topic review | implemented | `EmployeeHomeScenarioTests` |
| TC-INT-032 | Employee home head | pending head topic | implemented | `EmployeeHomeScenarioTests` |
| TC-INT-033 | Guidance alignment | declare work blocked reason | implemented | `GuidanceAlignmentScenarioTests` |
| TC-INT-034 | Admin preview SetUser | redirect Student/Diploma | implemented | `AdminPreviewEndpointTests` |
| TC-INT-035 | Secretary Dashboard HTTP | 200 OK | implemented | `SecretaryDashboardEndpointTests` |
| TC-INT-036 | Secretary Details HTTP | 200 OK | implemented | `SecretaryDashboardEndpointTests` |
| TC-INT-037 | Checkpoint empty document | DomainException | implemented | `AdmissionCheckpointEdgeScenarioTests` |
| TC-INT-038 | Out-of-order checkpoint | DomainException | implemented | `AdmissionCheckpointEdgeScenarioTests` |
| TC-INT-039 | Submit topic без supervisor | DomainException | implemented | `SubmitTopicWithoutSupervisorScenarioTests` |
| TC-INT-040 | Archive session | Archived + audit | implemented | `DefenceSessionArchiveScenarioTests` |
| TC-INT-041 | Secretary guidance assign reviewer | blocked reason | implemented | `GuidanceAlignmentScenarioTests` |
| TC-INT-042 | Employee home formatting | pending count | implemented | `EmployeeHomeScenarioTests` |
| TC-INT-043 | Employee home reviewer | pending count | implemented | `EmployeeHomeScenarioTests` |
| TC-INT-044 | Employee home anti-plagiarism | pending count | implemented | `EmployeeHomeScenarioTests` |
| TC-INT-045 | Secretary admit guidance | blocked reason | implemented | `GuidanceAlignmentScenarioTests` |
| TC-INT-046 | Secretary override guidance | blocked reason | implemented | `GuidanceAlignmentScenarioTests` |
| TC-INT-047 | Archive blocks student upload | DomainException | implemented | `ArchivedSessionScenarioTests` |
| TC-INT-048 | Secretary list HTTP | 200 + student | implemented | `SecretaryDiplomaListEndpointTests` |
| TC-INT-049 | Secretary list HTTP search | filter works | implemented | `SecretaryDiplomaListEndpointTests` |
| TC-INT-050 | Archive blocks declare work ready | DomainException | implemented | `ArchivedSessionScenarioTests.ArchivedSession_BlocksDeclareWorkReady` |
| TC-INT-051 | Secretary admitted report HTTP | 200 + student | implemented | `SecretaryReportsEndpointTests.GetAdmittedReport_...` |
| TC-INT-052 | Secretary admitted CSV HTTP | text/csv + student | implemented | `SecretaryReportsEndpointTests.GetAdmittedCsv_...` |
| TC-INT-053 | Supervisor checkpoint HTTP | redirect + FormattingReview | implemented | `SupervisorCheckpointEndpointTests` |
| TC-INT-054 | Guidance admit partial checkpoints | blocked reason | implemented | `GuidanceAlignmentScenarioTests.AfterPartialCheckpoints_...` |
| TC-INT-055 | Document view URL + file on disk | local-files path exists | implemented | `DocumentDownloadScenarioTests` |
| TC-INT-056 | GET document download authenticated | 200 + file bytes | implemented | `DocumentDownloadEndpointTests.GetDownload_Authenticated_...` |
| TC-INT-057 | GET document download unauthenticated | redirect login | implemented | `DocumentDownloadEndpointTests.GetDownload_Unauthenticated_...` |
| TC-INT-058 | GET document download missing file | 404 | implemented | `DocumentDownloadEndpointTests.GetDownload_MissingFile_...` |
| TC-INT-059 | DefenceSession GetDetails | groups + counts | implemented | `DefenceSessionAdminScenarioTests` |
| TC-INT-060 | DefenceSession GetAll | session in list | implemented | `DefenceSessionAdminScenarioTests` |
| TC-INT-061 | DefenceSession Update active | fields updated | implemented | `DefenceSessionAdminScenarioTests` |
| TC-INT-062 | DefenceSession Update archived | DomainException | implemented | `DefenceSessionAdminScenarioTests` |
| TC-INT-063 | Secretary CanAccess assigned | true | implemented | `SecretaryAccessScenarioTests` |
| TC-INT-064 | Secretary CanAccess other session | false | implemented | `SecretaryAccessScenarioTests` |
| TC-INT-065 | Secretary accessible sessions | includes assigned | implemented | `SecretaryAccessScenarioTests` |
| TC-INT-066 | IsSecretary for student | false | implemented | `SecretaryAccessScenarioTests` |
| TC-INT-067 | Student admin GetAll filter | seeded student | implemented | `StudentAdminScenarioTests` |
| TC-INT-068 | Student admin GetDetails | has diploma | implemented | `StudentAdminScenarioTests` |
| TC-INT-069 | Student admin Update | name changed | implemented | `StudentAdminScenarioTests` |
| TC-INT-070 | Secretary wrong session cookie HTTP | redirect Select | implemented | `SecretaryAccessEndpointTests` |
| TC-INT-071 | Formatting checkpoint HTTP | AntiPlagiarism step | implemented | `FormattingCheckpointEndpointTests` |
| TC-INT-072 | Reviewer checkpoint HTTP | ReadyForAdmission | implemented | `ReviewerCheckpointEndpointTests` |
| TC-INT-073 | Student POST UploadWork HTTP | document stored | implemented | `StudentDiplomaEndpointTests` |
| TC-INT-074 | Student POST DeclareWorkReady HTTP | DocumentsInProgress | implemented | `StudentDiplomaEndpointTests` |
| TC-INT-075 | Admin POST student import HTTP | imported count | implemented | `ImportEndpointTests` |
| TC-INT-076 | Employee admin create/update | persisted | implemented | `EmployeeAdminScenarioTests` |
| TC-INT-077 | Study group update | name changed | implemented | `StudyGroupAdminScenarioTests` |
| TC-INT-078 | Study group delete empty | removed | implemented | `StudyGroupAdminScenarioTests` |
| TC-INT-079 | Secretary list combo filter | lifecycle + step | implemented | `SecretaryDiplomaListScenarioTests` |
| TC-INT-080 | Local storage duplicate filename | `_2` suffix | implemented | `LocalFileStorageScenarioTests` |
| TC-INT-081 | AdmissionStepQueries FindWritable | tracked attempt | implemented | `AdmissionCheckpointEdgeScenarioTests` |
| TC-INT-082 | Anti-plagiarism checkpoint HTTP | ReviewerAssignment step | implemented | `AntiPlagiarismCheckpointEndpointTests` |
| TC-INT-083 | Head POST ApproveTopic HTTP | topic Approved | implemented | `DepartmentHeadTopicEndpointTests` |
| TC-INT-084 | Supervisor POST Confirm HTTP | Confirmed | implemented | `SupervisorWorkflowEndpointTests` |
| TC-INT-085 | Supervisor POST ApproveTopic HTTP | PendingHead | implemented | `SupervisorWorkflowEndpointTests` |
| TC-INT-086 | Secretary POST AssignReviewer HTTP | ExternalReview | implemented | `SecretaryDiplomaActionEndpointTests` |
| TC-INT-087 | Secretary POST AddComment HTTP | comment in history | implemented | `SecretaryDiplomaActionEndpointTests` |
| TC-INT-088 | Secretary POST OverrideSupervisor HTTP | supervisor changed | implemented | `SecretaryDiplomaActionEndpointTests` |
| TC-INT-089 | Secretary POST Session Select HTTP | redirect Dashboard | implemented | `SecretarySessionEndpointTests` |
| TC-APP-IMP-010 | Parse students xlsx with header | rows parsed | implemented | `ImportFileParserTests.ParseStudentsFromXlsx_WithHeader_...` |
| TC-APP-IMP-011 | Parse students xlsx english header | header skipped | implemented | `ImportFileParserTests.ParseStudentsFromXlsx_WithEnglishNameHeader_...` |
| TC-APP-IMP-012 | Parse students xlsx no header | first row as data | implemented | `ImportFileParserTests.ParseStudentsFromXlsx_WithoutRecognizedHeader_...` |
| TC-APP-IMP-013 | Parse students xlsx missing fields | parse error | implemented | `ImportFileParserTests.ParseStudentsFromXlsx_MissingRequiredFields_...` |
| TC-APP-IMP-014 | Parse employees xlsx valid | rows parsed | implemented | `ImportFileParserTests.ParseEmployeesFromXlsx_ValidRows_...` |
| TC-APP-IMP-015 | Parse employees xlsx missing fields | parse error | implemented | `ImportFileParserTests.ParseEmployeesFromXlsx_MissingRequiredFields_...` |
| TC-INT-090 | EmployeeHomeQueries pending supervisor students | count 1 | implemented | `EmployeeHomeQueriesScenarioTests` |
| TC-INT-091 | EmployeeHomeQueries pending supervisor topics | count 1 | implemented | `EmployeeHomeQueriesScenarioTests` |
| TC-INT-092 | EmployeeHomeQueries HasAnySupervisorDiplomas | true | implemented | `EmployeeHomeQueriesScenarioTests` |
| TC-INT-093 | EmployeeHomeQueries pending head topics | count 1 / empty sessions 0 | implemented | `EmployeeHomeQueriesScenarioTests` |
| TC-INT-094 | EmployeeHomeQueries pending checkpoints | supervisor/formatting/anti-plag/reviewer | implemented | `EmployeeHomeQueriesScenarioTests` |
| TC-INT-095 | EmployeeHomeQueries HasAnyReviewerDiplomas | true | implemented | `EmployeeHomeQueriesScenarioTests` |
| TC-INT-096 | Student import session not found | error message | implemented | `StudentImport_WhenSessionNotFound_ReturnsError` |
| TC-INT-097 | Student import archived session | error message | implemented | `StudentImport_WhenSessionArchived_ReturnsError` |
| TC-INT-098 | Student import unsupported format | error message | implemented | `StudentImport_UnsupportedFileFormat_ReturnsError` |
| TC-INT-099 | Employee import unsupported format | error message | implemented | `EmployeeImport_UnsupportedFileFormat_ReturnsError` |
| TC-INT-100 | Secretary list unknown session | `null` | implemented | `GetList_WhenSessionNotFound_ReturnsNull` |
| TC-INT-101 | Secretary list study group filter | matching diploma | implemented | `GetList_FilterByStudyGroup_ReturnsMatchingDiploma` |
| TC-INT-102 | Secretary list admission filter | NotAdmitted only | implemented | `GetList_FilterByAdmissionStatus_ReturnsNotAdmittedOnly` |
