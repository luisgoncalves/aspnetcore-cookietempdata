using AspNetCore.Mvc.CookieTempData.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using Xunit;

namespace AspNetCore.Mvc.CookieTempData.Tests
{
    public class CookieTempDataProviderTests
    {
        private readonly CookieTempDataOptions _options;
        private readonly Mock<IOptions<CookieTempDataOptions>> _optionsMock;
        private readonly Mock<IDataProtectionProvider> _dataProtectionProviderMock;
        private readonly Mock<IDataProtector> _dataProtectorMock;
        private readonly Mock<IBsonSerializer> _serializerMock;
        private readonly Mock<HttpContext> _contextMock;
        private readonly Mock<HttpRequest> _requestMock;
        private readonly Mock<IResponseCookies> _responseCookiesMock;

        public CookieTempDataProviderTests()
        {
            _options = new CookieTempDataOptions();
            _optionsMock = new Mock<IOptions<CookieTempDataOptions>>();
            _optionsMock.SetupGet(o => o.Value).Returns(_options);

            _dataProtectionProviderMock = new Mock<IDataProtectionProvider>(MockBehavior.Strict);
            _dataProtectorMock = new Mock<IDataProtector>(MockBehavior.Strict);
            _dataProtectionProviderMock
                .Setup(p => p.CreateProtector(It.IsAny<string>()))
                .Returns(_dataProtectorMock.Object);

            _serializerMock = new Mock<IBsonSerializer>(MockBehavior.Strict);

            _contextMock = new Mock<HttpContext>(MockBehavior.Strict);
            _requestMock = new Mock<HttpRequest>();
            var responseMock = new Mock<HttpResponse>();
            _responseCookiesMock = new Mock<IResponseCookies>(MockBehavior.Strict);

            _contextMock.SetupGet(c => c.Request).Returns(_requestMock.Object);
            _contextMock.SetupGet(c => c.Response).Returns(responseMock.Object);
            responseMock.SetupGet(r => r.Cookies).Returns(_responseCookiesMock.Object);
        }

        [Fact]
        public void Load_Without_Cookie_Returns_Null()
        {
            SetupRequestMock();

            var sut = CreateCookieTempDataProvider();
            var values = sut.LoadTempData(_contextMock.Object);

            Assert.Null(values);
        }

        [Fact]
        public void Load_With_Invalid_Base64_Cookie_Returns_Null_And_Removes_Cookie()
        {
            var cookies = new Dictionary<string, string>
            {
                { _options.CookieName, "ZZZXXXCCC" }
            };
            SetupRequestMock(cookies: cookies);
            SetupDeleteResponseCookieVerifiable();

            var sut = CreateCookieTempDataProvider();
            var values = sut.LoadTempData(_contextMock.Object);

            Assert.Null(values);
            _responseCookiesMock.Verify();
        }

        /// <remarks>
        /// This test also covers different users (different data protection purposes) because the same
        /// exception is used by data protection APIs.
        /// </remarks>
        [Fact]
        public void Load_With_Tampered_Cookie_Returns_Null_And_Removes_Cookie()
        {
            var cookies = new Dictionary<string, string>
            {
                { _options.CookieName, "Zm9vNDI=" /* valid base 64 */ }
            };
            SetupRequestMock(cookies: cookies);

            _dataProtectorMock
                .Setup(p => p.Unprotect(It.IsAny<byte[]>()))
                .Throws<CryptographicException>()
                .Verifiable();

            SetupDeleteResponseCookieVerifiable();

            var sut = CreateCookieTempDataProvider();
            var values = sut.LoadTempData(_contextMock.Object);

            Assert.Null(values);
            // An attempt to unprotect must have been made
            _dataProtectorMock.Verify();
            // Cookie must have been removed
            _responseCookiesMock.Verify();
        }

        [Fact]
        public void Load_With_Valid_Cookie_Returns_Values()
        {
            var cookies = new Dictionary<string, string>
            {
                { _options.CookieName, "Zm9vNDI=" /* valid base 64 */ }
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

            var sut = CreateCookieTempDataProvider();
            var values = sut.LoadTempData(_contextMock.Object);

            Assert.Same(markerValues, values);
            Assert.Equal(1, values.Count);
            Assert.True(values.ContainsKey("mykey"));
            Assert.Equal("myvalue", values["mykey"]);
        }

        [Fact]
        public void Save_Without_Values_And_Without_Cookie_Does_Nothing()
        {
            SetupRequestMock();

            var sut = CreateCookieTempDataProvider();
            sut.SaveTempData(_contextMock.Object, new Dictionary<string, object>(0));
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public void Save_Without_Values_But_With_Cookie_Removes_Cookie(bool https)
        {
            var cookies = new Dictionary<string, string>
            {
                { _options.CookieName, "X" }
            };
            SetupRequestMock(cookies: cookies, https: https);
            SetupDeleteResponseCookieVerifiable(https);

            var sut = CreateCookieTempDataProvider();
            sut.SaveTempData(_contextMock.Object, new Dictionary<string, object>(0));

            // Cookie must have been removed
            _responseCookiesMock.Verify();
        }

        [InlineData(true, null)]
        [InlineData(true, "/vdir")]
        [InlineData(false, null)]
        [InlineData(false, "/vdir/app")]
        [Theory]
        public void Save_With_Values_Sets_Cookie_With_Correct_Properties(bool https, string basePath)
        {
            SetupRequestMock(https: https, basePath: basePath);
            TestSuccessfulSave(https, basePath);
        }

        [Fact]
        public void Save_Creates_Specific_Data_Protector_If_User_Is_Authenticated()
        {
            SetupRequestMock(authenticatedUser: true);

            var currentUsername = _contextMock.Object.User.Identity.Name;
            _dataProtectorMock
                .Setup(p => p.CreateProtector(currentUsername))
                .Returns(_dataProtectorMock.Object)
                .Verifiable();

            TestSuccessfulSave();
        }

        private void TestSuccessfulSave(bool expectedCookieSecure = false, string expectedCookiePath = null)
        {
            var values = new Dictionary<string, object>
            {
                { "mykey", "myvalue" }
            };
            var markerBytes = new byte[0];

            _serializerMock
                .Setup(s => s.Serialize(values))
                .Returns(markerBytes);

            _dataProtectorMock
                .Setup(p => p.Protect(markerBytes))
                .Returns(markerBytes)
                .Verifiable();

            _responseCookiesMock
                .Setup(c => c.Append(_options.CookieName, It.IsAny<string>(), It.Is<CookieOptions>(o =>
                    o.Secure == expectedCookieSecure &&
                    o.HttpOnly == true &&
                    o.Path == (expectedCookiePath ?? "/"))))
                .Verifiable();

            var sut = CreateCookieTempDataProvider();
            sut.SaveTempData(_contextMock.Object, values);

            // Protect must have been invoked
            _dataProtectorMock.Verify();
            // Cookie must have been set
            _responseCookiesMock.Verify();
        }

        private void SetupRequestMock(bool https = false, Dictionary<string, string> cookies = null, bool authenticatedUser = false, string basePath = null)
        {
            var identity = authenticatedUser
                ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, Guid.NewGuid().ToString()) }, nameof(CookieTempDataProviderTests))
                : new ClaimsIdentity();

            _contextMock.SetupGet(c => c.User).Returns(new ClaimsPrincipal(identity));

            _requestMock.SetupGet(r => r.IsHttps).Returns(https);
            _requestMock.SetupGet(r => r.PathBase).Returns(basePath != null ? new PathString(basePath) : default(PathString));
            _requestMock.SetupGet(r => r.Cookies).Returns(new RequestCookieCollection(cookies ?? new Dictionary<string, string>(0)));
        }

        private void SetupDeleteResponseCookieVerifiable(bool https = false)
        {
            _responseCookiesMock
                .Setup(c => c.Delete(_options.CookieName, It.Is<CookieOptions>(o => o.Secure == https && o.HttpOnly == true && o.Path == "/")))
                .Verifiable();
        }

        private CookieTempDataProvider CreateCookieTempDataProvider() => new CookieTempDataProvider(_optionsMock.Object, _serializerMock.Object, _dataProtectionProviderMock.Object);
    }
}
