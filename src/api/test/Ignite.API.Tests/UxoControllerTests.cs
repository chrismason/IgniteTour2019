using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Ignite.API.Common.UXO;
using Ignite.API.Controllers;
using Ignite.API.Models;
using Ignite.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Ignite.API.Tests
{
    public class UxoControllerTests
    {
        private ILogger<UxoController> _logger;

        public UxoControllerTests()
        {
            _logger = new NullLogger<UxoController>();    
        }

        [Fact]
        public async Task UXO_GetAllWithNoExceptions_ShouldReturnOk()
        {
            var mockUxoService = new Mock<IUXOService>();
            var mockUxoDocumentService = new Mock<IUXODocumentService>();

            mockUxoService.Setup(svc => svc.GetUXOsForDisplay())
                .ReturnsAsync(SampleData.SampleMapItems());

            var controller = new UxoController(mockUxoService.Object, mockUxoDocumentService.Object, _logger);
            var result = await controller.GetUXOs();
            
            var actionResult = Assert.IsType<ActionResult<List<UXOMapItem>>>(result);
            var objectResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.IsType<List<UXOMapItem>>(objectResult.Value);
        }

        [Fact]
        public async Task UXO_GetAllHasAnError_ShouldReturnInternalServerError()
        {
            var mockUxoService = new Mock<IUXOService>();
            var mockUxoDocumentService = new Mock<IUXODocumentService>();

            mockUxoService.Setup(svc => svc.GetUXOsForDisplay())
                .ThrowsAsync(new Exception());

            var controller = new UxoController(mockUxoService.Object, mockUxoDocumentService.Object, _logger);
            var result = await controller.GetUXOs();
            
            var actionResult = Assert.IsType<ActionResult<List<UXOMapItem>>>(result);
            var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, objectResult.StatusCode);
        }

        [Fact]
        public async Task UXO_GetUXOById_ShouldReturnOk()
        {
            var uxoId = "1";

            var mockUxoService = new Mock<IUXOService>();
            var mockUxoDocumentService = new Mock<IUXODocumentService>();

            mockUxoService.Setup(svc => svc.FetchUXO(uxoId))
                .ReturnsAsync(SampleData.MinimalUXO());

            var controller = new UxoController(mockUxoService.Object, mockUxoDocumentService.Object, _logger);
            var result = await controller.GetUXOData(uxoId);

            var actionResult = Assert.IsType<ActionResult<UXO>>(result);
            var objectResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.IsType<UXO>(objectResult.Value);
        }

        [Fact]
        public async Task UXO_GetUXOByInvalidId_ShouldReturnNotFound()
        {
            var uxoId = "1";

            var mockUxoService = new Mock<IUXOService>();
            var mockUxoDocumentService = new Mock<IUXODocumentService>();

            mockUxoService.Setup(svc => svc.FetchUXO(uxoId))
                .ReturnsAsync((UXO)null);

            var controller = new UxoController(mockUxoService.Object, mockUxoDocumentService.Object, _logger);
            var result = await controller.GetUXOData(uxoId);

            var actionResult = Assert.IsType<ActionResult<UXO>>(result);
            var objectResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
            Assert.IsType<ErrorResponse>(objectResult.Value);
        }

        [Fact]
        public async Task UXO_GetUXOByIdHasAnError_ShouldReturnInternalServerError()
        {
            var uxoId = "1";

            var mockUxoService = new Mock<IUXOService>();
            var mockUxoDocumentService = new Mock<IUXODocumentService>();

            mockUxoService.Setup(svc => svc.FetchUXO(uxoId))
                .ThrowsAsync(new Exception());

            var controller = new UxoController(mockUxoService.Object, mockUxoDocumentService.Object, _logger);
            var result = await controller.GetUXOData(uxoId);

            var actionResult = Assert.IsType<ActionResult<UXO>>(result);
            var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, objectResult.StatusCode);
        }

        [Fact]
        public async Task UXO_GenerateDocumentWithValidId_ShouldReturnFile()
        {
            var uxoId = "1";

            var mockUxoService = new Mock<IUXOService>();
            var mockUxoDocumentService = new Mock<IUXODocumentService>();

            mockUxoService.Setup(svc => svc.FetchUXO(uxoId))
                .ReturnsAsync(SampleData.MinimalUXO());
            mockUxoDocumentService.Setup(svc => svc.CreateDocument(It.IsAny<UXO>()))
                .ReturnsAsync(SampleData.SampleFile());

            var controller = new UxoController(mockUxoService.Object, mockUxoDocumentService.Object, _logger);
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            var result = await controller.GenerateDocument(uxoId);

            var actionResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("APPLICATION/octet-stream", actionResult.ContentType);
        }

        [Fact]
        public async Task UXO_GenerateDocumentWithInvalidId_ShouldReturnNotFound()
        {
            var uxoId = "1";

            var mockUxoService = new Mock<IUXOService>();
            var mockUxoDocumentService = new Mock<IUXODocumentService>();

            mockUxoService.Setup(svc => svc.FetchUXO(uxoId))
                .ReturnsAsync((UXO)null);

            var controller = new UxoController(mockUxoService.Object, mockUxoDocumentService.Object, _logger);
            var result = await controller.GenerateDocument(uxoId);

            var actionResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.IsType<ErrorResponse>(actionResult.Value);
        }

        [Fact]
        public async Task UXO_GenerateDocumentFailsToProduceDocument_ShouldReturnInternalServerError()
        {
            var uxoId = "1";

            var mockUxoService = new Mock<IUXOService>();
            var mockUxoDocumentService = new Mock<IUXODocumentService>();

            mockUxoService.Setup(svc => svc.FetchUXO(uxoId))
                .ReturnsAsync(SampleData.MinimalUXO());
            mockUxoDocumentService.Setup(svc => svc.CreateDocument(It.IsAny<UXO>()))
                .ReturnsAsync((byte[])null);

            var controller = new UxoController(mockUxoService.Object, mockUxoDocumentService.Object, _logger);
            var result = await controller.GenerateDocument(uxoId);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, objectResult.StatusCode);
        }
    }
}
