using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using Ignite.API.Common.UXO;
using Ignite.API.Models;
using Ignite.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Ignite.API.Controllers
{
    [Authorize]
    [Route("recons/uxo/[controller]")]
    [ApiController]
    public class UxoController : ControllerBase
    {
        private readonly IUXOService _uxoService;
        private readonly IUXODocumentService _documentService;
        private readonly ILogger<UxoController> _logger;

        public UxoController(IUXOService uxoService, IUXODocumentService uxoDocumentService, ILogger<UxoController> logger)
        {
            _uxoService = uxoService;
            _documentService = uxoDocumentService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<UXOMapItem>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<List<UXOMapItem>>> GetUXOs()
        {
            try
            {
                var data = await _uxoService.GetUXOsForDisplay();
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get UXOs failed. Message=[{ex.Message}]");
                return StatusCode(
                    (int)HttpStatusCode.InternalServerError, 
                    new ErrorResponse() { StatusCode = (int)HttpStatusCode.InternalServerError, Message = "Error retrieving data"}
                );
            }
        }

        [HttpGet("{uxoid}/details")]
        [ProducesResponseType(typeof(UXO), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<UXO>> GetUXOData(string uxoid)
        {
            try
            {
                var uxo = await _uxoService.FetchUXO(uxoid);
                if (uxo == null)
                {
                    return NotFound(new ErrorResponse() { StatusCode = (int)HttpStatusCode.NotFound, Message = $"Unable to retrieve item '{uxoid}'"});
                }
                return Ok(uxo);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get UXO failed. Message=[{ex.Message}]");
                return StatusCode(
                    (int)HttpStatusCode.InternalServerError,
                    new ErrorResponse() { StatusCode = (int)HttpStatusCode.InternalServerError, Message = $"Unable to retrieve item '{uxoid}'"}
                );
            }
        }

        [HttpPost("{uxoid}/documents/create")]
        [ProducesResponseType(typeof(FileContentResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult> GenerateDocument(string uxoid)
        {
            var uxo = await _uxoService.FetchUXO(uxoid);
            if (uxo == null)
            {
                return NotFound(new ErrorResponse() { StatusCode = (int)HttpStatusCode.NotFound, Message = $"Unable to retrieve item '{uxoid}'"});
            }

            var fileBytes = await _documentService.CreateDocument(uxo);
            if (fileBytes == null || fileBytes.Length == 0)
            {
                return StatusCode(
                    (int)HttpStatusCode.InternalServerError,
                    new ErrorResponse() { StatusCode = (int)HttpStatusCode.InternalServerError, Message = "Unable to generate the document."}
                );
            }

            var fileName = $"UXO-{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mmZ")}.docx";

            var contentDisposition = new ContentDisposition()
            {
                FileName = fileName,
                Inline = false
            };

            Response.Headers.Add("Content-Disposition", contentDisposition.ToString());

            var contentType = "APPLICATION/octet-stream";
            return File(fileBytes, contentType);
        }
    }
}