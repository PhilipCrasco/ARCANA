using Microsoft.AspNetCore.Mvc;
using RDF.Arcana.API.Common;
using RDF.Arcana.API.Common.Extension;
using RDF.Arcana.API.Common.Pagination;
using RDF.Arcana.API.Data;
using RDF.Arcana.API.Domain;

namespace RDF.Arcana.API.Features.Setup.Booking_Coverage;

[Route("api/BookingCoverage")]
[ApiController]

public class GetAllBookingCoverages : ControllerBase
{
    private readonly IMediator _mediator;

    public GetAllBookingCoverages(IMediator mediator)
    {
        _mediator = mediator;
    }

    public class GetAllBookingCoveragesQuery : UserParams, IRequest<PagedList<GetAllBookingCoveragesResult>>
    {
        public string Search { get; set; }
        public bool? Status { get; set; }
    }

    public class GetAllBookingCoveragesResult
    {
        public int Id { get; set; }
        public string BookingCoverage { get; set; }
    }
    
    public class Handler : IRequestHandler<GetAllBookingCoveragesQuery, PagedList<GetAllBookingCoveragesResult>>
    {
        private readonly DataContext _context;

        public Handler(DataContext context)
        {
            _context = context;
        }

        public async Task<PagedList<GetAllBookingCoveragesResult>> Handle(GetAllBookingCoveragesQuery request, CancellationToken cancellationToken)
        {
            IQueryable<BookingCoverages> bookingCoverages = _context.BookingCoverages
                .Include(x => x.AddedByUser);

            if (!string.IsNullOrEmpty(request.Search))
            {
                bookingCoverages = bookingCoverages.Where(x => x.BookingCoverage.Contains(request.Search));
            }

            if (request.Status != null)
            {
                bookingCoverages = bookingCoverages.Where(x => x.IsActive == request.Status);
            }

            var result = bookingCoverages.Select(x => x.ToGetAllBookingCoveragesResult());
            return await PagedList<GetAllBookingCoveragesResult>.CreateAsync(result, request.PageNumber,
                request.PageSize);
        }
    }

    [HttpGet("GetAllBookingCoverages")]
    public async Task<IActionResult> Get([FromQuery] GetAllBookingCoveragesQuery query)
    {
        var response = new QueryOrCommandResult<object>();
        try
        {
            var bookingCoverages = await _mediator.Send(query);

            Response.AddPaginationHeader(
                bookingCoverages.CurrentPage,
                bookingCoverages.PageSize,
                bookingCoverages.TotalCount,
                bookingCoverages.TotalPages,
                bookingCoverages.HasPreviousPage,
                bookingCoverages.HasNextPage
            );

            var result = new QueryOrCommandResult<object>
            {
                Success = true,
                Status = StatusCodes.Status200OK,
                Data = new
                {
                    requestedProspect = bookingCoverages,
                    bookingCoverages.CurrentPage,
                    bookingCoverages.PageSize,
                    bookingCoverages.TotalCount,
                    bookingCoverages.TotalPages,
                    bookingCoverages.HasPreviousPage,
                    bookingCoverages.HasNextPage
                }
            };

            result.Messages.Add("Successfully Fetch Data");

            return Ok(result);
        }
        catch (Exception e)
        {
            response.Messages.Add(e.Message);
            response.Status = StatusCodes.Status409Conflict;

            return Ok(response);
        }
    }
}