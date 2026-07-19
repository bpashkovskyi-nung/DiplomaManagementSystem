using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Persistence.Contracts;

using DiplomaManagementSystem.Application.Secretary;

using DiplomaManagementSystem.Application.Secretary.Dtos;

using DiplomaManagementSystem.Domain.Entities;

namespace DiplomaManagementSystem.Application.Employee;

internal sealed class SupervisorDiplomaDetailsService(

    IDiplomaQueries diplomaQueries,

    DiplomaDetailsAssembler diplomaDetailsAssembler) : ISupervisorDiplomaDetailsService

{

    public async Task<DiplomaDetailsDto?> GetDetailsAsync(

        Guid supervisorId,

        Guid diplomaId,

        CancellationToken cancellationToken = default)

    {

        Diploma? diploma = await diplomaQueries.FindForSupervisorReadAsync(

            supervisorId,

            diplomaId,

            cancellationToken);

        if (diploma is null)

        {

            return null;

        }

        DiplomaDetailsContext context = await diplomaDetailsAssembler.LoadContextAsync(diploma, cancellationToken);

        DiplomaDetailsHistory history = DiplomaDetailsAssembler.BuildHistory(context);

        DiplomaDetailsScreenParts screenParts = DiplomaDetailsAssembler.BuildReadOnlyScreenParts(context);

        return DiplomaDetailsAssembler.Assemble(diploma.DefenceSessionId, context, history, screenParts);

    }

}

