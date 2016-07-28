using AspNetCore.Mvc.CookieTempData.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using Xunit;

namespace AspNetCore.Mvc.CookieTempData.Tests
{
    public class CookieTempDataProviderTests
    {
        private readonly Mock<IDataProtectionProvider> _dataProtectionProviderMock;
        private readonly Mock<IDataProtector> _dataProtectorMock;
        private readonly Mock<IBsonSerializer> _serializerMock;
        private readonly Mock<HttpContext> _contextMock;
        private readonly Mock<HttpRequest> _requestMock;
        private readonly Mock<IResponseCookies> _responseCookiesMock;

        public CookieTempDataProviderTests()
        {
            _dataProtectionProviderMock = new Mock<IDataProtectionProvider>(MockBehavior.Strict);
            _dataProtectorMock = new Mock<IDataProtector>(MockBehavior.Strict);
            // Initial creation
            _dataProtectionProviderMock
                .Setup(p => p.CreateProtector(It.IsAny<string>()))
                .Returns(_dataProtectorMock.Object);
            // Nested creation
            //_dataProtectorMock
            //    .Setup(p => p.CreateProtector(It.IsAny<string>()))
            //    .Returns(_dataProtectorMock.Object);

            _serializerMock = new Mock<IBsonSerializer>(MockBehavior.Strict);

            _contextMock = new Mock<HttpContext>(MockBehavior.Strict);
            _requestMock = new Mock<HttpRequest>();
            var responseMock = new Mock<HttpResponse>();
            _responseCookiesMock = new Mock<IResponseCookies>(MockBehavior.Strict);

            _contextMock.SetupGet(c => c.Request).Returns(_requestMock.Object);
            _contextMock.SetupGet(c => c.Response).Returns(responseMock.Object);
            _contextMock.SetupGet(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity()));
            _requestMock.SetupGet(r => r.PathBase);
            responseMock.SetupGet(r => r.Cookies).Returns(_responseCookiesMock.Object);
        }

        private void SetupRequestMock(bool https = false, Dictionary<string, string> cookies = null)
        {
            _requestMock.SetupGet(r => r.IsHttps).Returns(https);
            _requestMock.SetupGet(r => r.Cookies).Returns(new RequestCookieCollection(cookies ?? new Dictionary<string, string>(0)));
        }

        [Fact]
        public void Load_Without_Cookie_Returns_Null()
        {
            SetupRequestMock();

            var sut = new CookieTempDataProvider(_serializerMock.Object, _dataProtectionProviderMock.Object);
            var values = sut.LoadTempData(_contextMock.Object);

            Assert.Null(values);
        }

        [Fact]
        public void Load_With_Invalid_Base64_Cookie_Returns_Null_And_Removes_Cookie()
        {
            var cookies = new Dictionary<string, string>
            {
                { "tmp", "ZZZXXXCCC" }
            };
            SetupRequestMock(cookies: cookies);

            _responseCookiesMock
                .Setup(c => c.Delete("tmp", It.IsAny<CookieOptions>()))
                .Verifiable();

            var sut = new CookieTempDataProvider(_serializerMock.Object, _dataProtectionProviderMock.Object);
            var values = sut.LoadTempData(_contextMock.Object);

            Assert.Null(values);
            _responseCookiesMock.Verify();
        }

        [Fact]
        public void Load_With_Tampered_Cookie_Returns_Null_And_Removes_Cookie()
        {
            // This test also covers different users (different data protection purposes) because
            // the same exception is used by data protection APIs.

            var cookies = new Dictionary<string, string>
            {
                { "tmp", "Zm9vNDI=" /* valid base 64 */ }
            };
            SetupRequestMock(cookies: cookies);

            _dataProtectorMock
                .Setup(p => p.Unprotect(It.IsAny<byte[]>()))
                .Throws<CryptographicException>()
                .Verifiable();

            _responseCookiesMock
                .Setup(c => c.Delete("tmp", It.IsAny<CookieOptions>()))
                .Verifiable();

            var sut = new CookieTempDataProvider(_serializerMock.Object, _dataProtectionProviderMock.Object);
            var values = sut.LoadTempData(_contextMock.Object);

            Assert.Null(values);
            // An attempt to unprotect must have been made
            _dataProtectorMock.Verify();
            _responseCookiesMock.Verify();
        }

        [Fact]
        public void Load_With_Valid_Cookie_Returns_Values()
        {
            var cookies = new Dictionary<string, string>
            {
                { "tmp", "Zm9vNDI=" /* valid base 64 */ }
            };
            SetupRequestMock(cookies: cookies);

            var markerBytes = new byte[0];
            var markerValues = new Dictionary<string, object>
            {
                { "mykey", "myvalue" }
            };

            _dataProtectorMock
                .Setup(p => p.Unprotect(It.IsAny<byte[]>()))
                .Returns(markerBytes);

            _serializerMock
                .Setup(s => s.Deserialize<IDictionary<string, object>>(markerBytes))
                .Returns(markerValues);

            var sut = new CookieTempDataProvider(_serializerMock.Object, _dataProtectionProviderMock.Object);
            var values = sut.LoadTempData(_contextMock.Object);

            Assert.Same(markerValues, values);
            Assert.Equal(1, values.Count);
            Assert.True(values.ContainsKey("mykey"));
            Assert.Equal("myvalue", values["mykey"]);
        }
    }
}
