using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Xunit;
using Moq;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Internal;

namespace AspNetCore.Mvc.CookieTempData.Tests
{
    public class CookieTempDataProviderTests
    {
        private readonly Mock<IDataProtectionProvider> _dataProtectionProviderMock;
        private readonly Mock<IDataProtector> _dataProtectorMock;

        public CookieTempDataProviderTests()
        {
            _dataProtectionProviderMock = new Mock<IDataProtectionProvider>(MockBehavior.Strict);
            _dataProtectorMock = new Mock<IDataProtector>(MockBehavior.Strict);
            // Initial creation
            _dataProtectionProviderMock
                .Setup(p => p.CreateProtector(It.IsAny<string>()))
                .Returns(_dataProtectorMock.Object);
            // Nested creation
            _dataProtectorMock
                .Setup(p => p.CreateProtector(It.IsAny<string>()))
                .Returns(_dataProtectorMock.Object);
        }

        private HttpContext CreateHttpContext(bool https = false, Dictionary<string, string> headers = null)
        {
            var contextMock = new Mock<HttpContext>(MockBehavior.Strict);
            var requestMock = new Mock<HttpRequest>();
            var responseMock = new Mock<HttpResponse>();

            contextMock.SetupGet(c => c.Request).Returns(requestMock.Object);
            contextMock.SetupGet(c => c.Response).Returns(responseMock.Object);
            contextMock.SetupGet(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity()));

            requestMock.SetupGet(r => r.PathBase);
            requestMock.SetupGet(r => r.IsHttps).Returns(https);
            requestMock.SetupGet(r => r.Cookies).Returns(new RequestCookieCollection(headers ?? new Dictionary<string, string>(0)));

            responseMock.SetupGet(r => r.Cookies).Returns(new ResponseCookies(new HeaderDictionary(), null));

            return contextMock.Object;
        }

        [Fact]
        public void Load_Without_Cookie_Returns_Null()
        {
            var context = CreateHttpContext();

            var sut = new CookieTempDataProvider(_dataProtectionProviderMock.Object);
            var values = sut.LoadTempData(context);

            Assert.Null(values);
        }

        //[Fact]
        //public void Load_With_Valid_Cookie_Returns_Values()
        //{

        //}
    }
}
