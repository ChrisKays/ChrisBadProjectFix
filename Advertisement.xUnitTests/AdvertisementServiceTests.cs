using System;
using System.Threading;
using System.Runtime.Caching;
using Adv;
using Moq;
using ThirdParty;

namespace Advertisement.xUnitTests
{
    public class AdvertisementServiceTests
    {
        private readonly MemoryCache _cache;
        private readonly Mock<NoSqlAdvProvider> _mainProvider;
        private readonly AdvertisementService _advertisementService;

        public AdvertisementServiceTests()
        {
            _cache = new MemoryCache("Content");
            _mainProvider = new Mock<NoSqlAdvProvider>();
            _advertisementService = new AdvertisementService(_cache, _mainProvider.Object, 5);
        }

        [Fact]
        public void GetAdvertisement_FromCache_Success()
        {
            // Arrange
            var id = "123";
            var advertisement = new ThirdParty.Advertisement { WebId = id, Name = "Test Content" };
            //_cache = new MemoryCache("Content");
            _cache.Set($"AdvKey_{id}", advertisement, DateTimeOffset.Now.AddMinutes(5));

            // Act
            var result = _advertisementService.GetAdvertisement(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(advertisement.WebId, result.WebId);
            Assert.Equal(advertisement.Name, result.Name);
        }

        [Fact]
        public void GetAdvertisement_FromMainProvider_Success()
        {
            // Arrange
            var id = "456";
            var advertisement = new ThirdParty.Advertisement { WebId = id, Name = $"Advertisement #{id}" };
            var setup = _mainProvider.Setup(provider => provider.GetAdv((It.IsAny<string>()))).Returns(advertisement);
                     
            // Act
            var result = _advertisementService.GetAdvertisement(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(advertisement.WebId, result.WebId);
            Assert.Equal(advertisement.Name, result.Name);
        }
    }
}