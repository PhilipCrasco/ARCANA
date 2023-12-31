using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RDF.Arcana.API.Common;
using RDF.Arcana.API.Data;
using RDF.Arcana.API.Features.Client.Prospecting.Released;
using RDF.Arcana.API.Features.Clients.Prospecting.Exception;

namespace RDF.Arcana.API.Features.Clients.Prospecting.Released;

[Route("api/[controller]")]
[ApiController]

public class ReleasedProspectingRequest : ControllerBase
{

    private readonly IMediator _mediator;

    public ReleasedProspectingRequest(IMediator mediator)
    {
        _mediator = mediator;
    }

    public class ReleasedProspectingRequestCommand : IRequest<Unit>
    {
        public int ClientId { get; set; }
        public IFormFile PhotoProof { get; set; }
        public IFormFile ESignature { get; set; }
    }

    public class Handler : IRequestHandler<ReleasedProspectingRequestCommand, Unit>
    {
        private const string released = "Released";
        private readonly DataContext _context;
        private readonly Cloudinary _cloudinary;

        public Handler(IOptions<CloudinarySettings> config, DataContext context)
        {
            var account = new Account(
                    config.Value.Cloudname,
                    config.Value.ApiKey,
                    config.Value.ApiSecret
                );

            _cloudinary = new Cloudinary( account );
            _context = context;
        }

        public async  Task<Unit> Handle(ReleasedProspectingRequestCommand request, CancellationToken cancellationToken)
        {
            var validateClientRequest = await _context.Approvals
                .Include(x => x.FreebieRequest)
                .Include(x => x.Client)
                .FirstOrDefaultAsync(x =>
                    x.ClientId == request.ClientId &&
                    x.ApprovalType == "For Freebie Approval" &&
                    x.IsActive == true &&
                    x.IsApproved == true, cancellationToken);
            
            if (validateClientRequest is null)
            {
                throw new NoProspectClientFound();
            }




            //if (request.PhotoProof != null)
            //{
            //    var savePath = Path.Combine($@"F:\images\{validateClientRequest.Client.BusinessName}", request.PhotoProof.FileName);

            //    var directory = Path.GetDirectoryName(savePath);
            //    if (directory != null && !Directory.Exists(directory))
            //        Directory.CreateDirectory(directory);

            //    await using var stream = System.IO.File.Create(savePath);
            //    await request.PhotoProof.CopyToAsync(stream, cancellationToken);

            //    validateClientRequest.FreebieRequest.PhotoProofPath = savePath;
            //}

            //if (request.ESignature != null)
            //{
            //    var savePath = Path.Combine($@"F:\images\{validateClientRequest.Client.BusinessName}", request.ESignature.FileName);

            //    var directory = Path.GetDirectoryName(savePath);
            //    if (directory != null && !Directory.Exists(directory))
            //        Directory.CreateDirectory(directory);

            //    await using var stream = System.IO.File.Create(savePath);
            //    await request.ESignature.CopyToAsync(stream, cancellationToken);

            //    validateClientRequest.FreebieRequest.ESignaturePath = savePath;
            //}

            if (request.PhotoProof.Length > 0 || request.ESignature.Length > 0)
            {
                await using var stream = request.PhotoProof.OpenReadStream();
                await using var esignatureStream = request.ESignature.OpenReadStream();

                var photoProofParams = new ImageUploadParams
                {
                    File = new FileDescription(request.PhotoProof.FileName, stream),
                    PublicId = $"{validateClientRequest.Client.BusinessName}/{request.PhotoProof.FileName}"
                };

                var eSignaturePhotoParams = new ImageUploadParams
                {
                    File = new FileDescription(request.ESignature.FileName, esignatureStream),
                    PublicId = $"{validateClientRequest.Client.BusinessName}/{request.ESignature.FileName}"
                };


                var photoproofUploadResult = await _cloudinary.UploadAsync(photoProofParams);
                var eSignatureUploadResult = await _cloudinary.UploadAsync(eSignaturePhotoParams);

                if (photoproofUploadResult.Error != null)
                {
                    throw new System.Exception(photoproofUploadResult.Error.Message);
                }

                if(eSignatureUploadResult.Error != null)
                {
                    throw new System.Exception(eSignatureUploadResult.Error.Message);
                }

                validateClientRequest.FreebieRequest.Status = "Released";
                validateClientRequest.FreebieRequest.IsDelivered = true;
                validateClientRequest.Client.RegistrationStatus = "Released";
                validateClientRequest.FreebieRequest.PhotoProofPath = photoproofUploadResult.SecureUrl.ToString();
                validateClientRequest.FreebieRequest.ESignaturePath = eSignatureUploadResult.SecureUrl.ToString();

                await _context.SaveChangesAsync(cancellationToken);

                return Unit.Value;

            }

            throw new System.Exception("Error");
        }

    }

    [HttpPut("ReleasedProspectingRequest/{id:int}")]
    public async Task<IActionResult> ReleasedProspecting([FromForm] ReleasedProspectingRequestCommand command,
        [FromRoute] int id)
    {
        var response = new QueryOrCommandResult<UploadPhotoResult>();
        try
        {
            command.ClientId = id;

            var result = await _mediator.Send(command);
            response.Status = StatusCodes.Status200OK;
            response.Messages.Add("Freebie Request has been released");
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