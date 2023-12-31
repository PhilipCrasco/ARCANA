using Microsoft.AspNetCore.Mvc;
using RDF.Arcana.API.Common;
using RDF.Arcana.API.Common.Extension;
using RDF.Arcana.API.Common.Pagination;
using RDF.Arcana.API.Data;
using RDF.Arcana.API.Domain;
using RDF.Arcana.API.Features.Clients.Prospecting.Request;


namespace RDF.Arcana.API.Features.Client.Direct;

[Route("api/DirectRegistration")]
[ApiController]
public class GetAllDirectRegistrationClients : ControllerBase
{
    private readonly IMediator _mediator;

    public GetAllDirectRegistrationClients(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("GetAllDirectRegistrationClients")]
    public async Task<IActionResult> GetAllDirectRegistrationClient(
        [FromQuery] GetAllDirectRegistrationClientsQuery query)
    {
        var response = new QueryOrCommandResult<object>();
        try
        {
            var directRegistrationClients = await _mediator.Send(query);

            Response.AddPaginationHeader(
                directRegistrationClients.CurrentPage,
                directRegistrationClients.PageSize,
                directRegistrationClients.TotalCount,
                directRegistrationClients.TotalPages,
                directRegistrationClients.HasPreviousPage,
                directRegistrationClients.HasNextPage
            );

            var result = new QueryOrCommandResult<object>
            {
                Success = true,
                Status = StatusCodes.Status200OK,
                Data = new
                {
                    directRegistrationClients,
                    directRegistrationClients.CurrentPage,
                    directRegistrationClients.PageSize,
                    directRegistrationClients.TotalCount,
                    directRegistrationClients.TotalPages,
                    directRegistrationClients.HasPreviousPage,
                    directRegistrationClients.HasNextPage
                }
            };

            result.Messages.Add("Successfully Fetch Data");

            return Ok(result);
        }
        catch (System.Exception e)
        {
            response.Messages.Add(e.Message);
            response.Status = StatusCodes.Status409Conflict;

            return Ok(response);
        }
    }

    public class GetAllDirectRegistrationClientsQuery : UserParams,
        IRequest<PagedList<GetAllDirectRegistrationClientsResult>>
    {
        public string Search { get; set; }
        public bool? Status { get; set; }
    }

    public class GetAllDirectRegistrationClientsResult
    {
        public int ClientId { get; set; }
        public string Fullname { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string BusinessName { get; set; }
        public string StoreType { get; set; }
        public string BusinessAddress { get; set; }
        public string RepresentativeName { get; set; }
        public string RepresentativePosition { get; set; }
        public int Cluster { get; set; }
        public bool Freezer { get; set; }
        public string ClientType { get; set; }
        public bool? DirectDelivery { get; set; }
        public string BookingCoverage { get; set; }
        public string ModeOfPayment { get; set; }
        public string RegistrationStatus { get; set; }
        public string CustomerType { get; set; }
        public bool IsActive { get; set; }
        public string AddedBy { get; set; }
        public string Longitude { get; set; }
        public string Latitude { get; set; }
        public bool? VariableDiscount { get; set; }
        public ICollection<ClientTerms> Terms { get; set; }
        public List<ClientAttachments> Attachments { get; set; }
        public decimal DiscountPercentage { get; set; }

        public class ClientTerms
        {
            public int TermId { get; set; }
            public string Term { get; set; }
            public int? CreditLimit { get; set; }
            public int TermDays { get; set; }
        }

        public class ClientAttachments
        {
            public string Attachment { get; set; }
        }

        public class Handlder : IRequestHandler<GetAllDirectRegistrationClientsQuery,
            PagedList<GetAllDirectRegistrationClientsResult>>
        {
            private readonly DataContext _context;

            public Handlder(DataContext context)
            {
                _context = context;
            }

            public async Task<PagedList<GetAllDirectRegistrationClientsResult>> Handle(
                GetAllDirectRegistrationClientsQuery request, CancellationToken cancellationToken)
            {
                IQueryable<Approvals> clients = _context.Approvals
                    .Include(x => x.Client)
                    .Include(x => x.FreebieRequest)
                    .ThenInclude(x => x.FreebieItems)
                    .Include(x => x.Client)
                    .ThenInclude(x => x.BookingCoverages)
                    .Include(x => x.Client)
                    .ThenInclude(x => x.Term)
                    .ThenInclude(x => x.TermOptions)
                    .ThenInclude(x => x.TermDays)
                    .Include(x => x.Client)
                    .ThenInclude(x => x.FixedDiscounts)
                    .Include(x => x.Client)
                    .ThenInclude(x => x.ClientDocuments)
                    .Include(x => x.Client)
                    .ThenInclude(x => x.StoreType)
                    .Include(x => x.RequestedByUser)
                    .Where(x => x.Client.CustomerType == "Direct");


                if (!string.IsNullOrEmpty(request.Search))
                {
                    clients = clients.Where(x =>
                        x.Client.Fullname == request.Search || x.Client.BusinessName == request.Search);
                }

                if (request.Status != null)
                {
                    clients = clients.Where(x => x.Client.IsActive == request.Status);
                }

                var result = clients.Select(x => x.ToGetAllDirectRegistrationClientsResult());

                return await PagedList<GetAllDirectRegistrationClientsResult>.CreateAsync(result, request.PageNumber,
                    request.PageSize);
            }
        }
    }
}