using ContainerDesktop.Common;
using ContainerDesktop.Common.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using System.Linq;

namespace Common.Test.Services
{
    [TestClass]
    public class WslServiceTest
    {
        [TestMethod]
        public void GetDistrosReturnsListFromRegistry()
        {
            // Arrange
            var processExecutorMock = new Mock<IProcessExecutor>();
            var loggerMock = new Mock<ILogger<WslService>>();
            var sut = new WslService(processExecutorMock.Object, loggerMock.Object);

            // Act
            var distros = sut.GetDistros();

            // Assert
            distros.Any().ShouldBeTrue();
            processExecutorMock.VerifyNoOtherCalls();
        }
    }
}
