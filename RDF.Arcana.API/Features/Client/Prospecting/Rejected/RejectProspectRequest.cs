﻿using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RDF.Arcana.API.Common;
using RDF.Arcana.API.Data;
using RDF.Arcana.API.Domain;

namespace RDF.Arcana.API.Features.Clients.Prospecting.Rejected;

[Route("api/Prospecting")]
[ApiController]

public class RejectProspectRequest : ControllerBase
{
    private readonly IMediator _mediator;

    public RejectProspectRequest(IMediator mediator)
    {
        _mediator = mediator;
    }

    public class RejectProspectRequestCommand : IRequest<Unit>
    {
        public int ProspectId { get; set; }
        public int RejectedBy { get; set; }
        public string Reason { get; set; }
    }

    public class Handler : IRequestHandler<RejectProspectRequestCommand, Unit>
    {
        private readonly DataContext _context;

        public Handler(DataContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(RejectProspectRequestCommand request, CancellationToken cancellationToken)
        {
            // Fetch the requested client by the prospectId
            var requestedClient =
                await _context.Approvals
                    .Include(x => x.Client)
                    .FirstOrDefaultAsync(
                    x => x.ClientId == request.ProspectId && 
                         x.ApprovalType == "Approver Approval" && 
                         x.IsApproved == false && 
                         x.IsActive == true,
                    cancellationToken);

            // If no matching client is found, throw an exception
            if (requestedClient is null)
            {
                throw new System.Exception("No matching client found");
            }

            if (requestedClient.Client.RegistrationStatus == "Rejected")
            {
                throw new System.Exception("This client is already rejected");
            }

            // Set the status to "rejected" or an equivalent indicator for rejection in your system
            // requestedClient.ApprovalType = "Rejected";
            requestedClient.Reason = request.Reason;
            requestedClient.Client.RegistrationStatus = "Rejected";
            // Save the changes to the database
            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }

    [HttpPut("RejectProspectRequest/{id:int}")]
    public async Task<IActionResult> Reject([FromRoute] int id, [FromBody]RejectProspectRequestCommand command)
    {
        var response = new QueryOrCommandResult<object>();
        try
        {
            command.ProspectId = id;
                
            if (User.Identity is ClaimsIdentity identity 
                && int.TryParse(identity.FindFirst("id")?.Value, out var userId))
            {
                command.RejectedBy = userId;
            }

            await _mediator.Send(command);
            response.Status = StatusCodes.Status200OK;
            response.Messages.Add("Client is rejected successfully");
            response.Success = true;
            return Ok(response);

        }
        catch (System.Exception e)
        {
            response.Messages.Add(e.Message);
            response.Status = StatusCodes.Status409Conflict;
            return Conflict(response);
        }
    }
}