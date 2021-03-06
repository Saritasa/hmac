﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Donker.Hmac.Configuration.Test
{
    [TestClass]
    public class HmacConfigurationManagerTests
    {
        private string _hmacConfig;
        private string _hmacModifiedConfig;

        [TestInitialize]
        public void Initialize()
        {
            using (StreamReader reader = new StreamReader("Hmac.config"))
                _hmacConfig = reader.ReadToEnd();

            _hmacModifiedConfig =
@"<?xml version='1.0' encoding='utf-8'?>
<donker.hmac>
  <configurations>
    <configuration name='TestConfiguration'
        authorizationScheme='TEST_MODIFIED'>
      <headers/>
    </configuration>
    <configuration name='TestConfiguration2'
        authorizationScheme='TEST_MODIFIED_2'>
      <headers/>
    </configuration>
  </configurations>
</donker.hmac>";
        }

        [TestMethod]
        public void ShouldConfigureFromXmlAppConfig()
        {
            // Arrange
            HmacConfigurationManager configurationManager = new HmacConfigurationManager();
            HmacConfiguration expectedConfiguration = new HmacConfiguration
            {
                Name = "TestConfiguration",
                UserHeaderName = "X-Test-User",
                AuthorizationScheme = "TEST",
                SignatureDataSeparator = "_",
                SignatureEncoding = "UTF-32",
                HmacAlgorithm = "HMACSHA256",
                MaxRequestAge = TimeSpan.FromMinutes(2),
                SignRequestUri = false,
                ValidateContentMd5 = true,
                Headers = new List<string> { "X-Test-Header-1", "X-Test-Header-2" }
            };

            // Act
            configurationManager.ConfigureFromXmlAppConfig();
            IHmacConfiguration configuration = configurationManager.Get(expectedConfiguration.Name);

            // Assert
            AssertConfiguration(expectedConfiguration, configuration);
        }

        [TestMethod]
        public void ShouldConfigureFromFile()
        {
            // Arrange
            HmacConfigurationManager configurationManager = new HmacConfigurationManager();
            HmacConfiguration expectedConfiguration = new HmacConfiguration
            {
                Name = "TestConfiguration",
                UserHeaderName = "X-Test-User",
                AuthorizationScheme = "TEST",
                SignatureDataSeparator = "_",
                SignatureEncoding = "UTF-32",
                HmacAlgorithm = "HMACSHA256",
                MaxRequestAge = TimeSpan.FromMinutes(2),
                SignRequestUri = false,
                ValidateContentMd5 = true,
                Headers = new List<string> { "X-Test-Header-1", "X-Test-Header-2" }
            };

            // Act
            configurationManager.ConfigureFromFile("Hmac.config");
            IHmacConfiguration configuration = configurationManager.Get(expectedConfiguration.Name);

            // Assert
            AssertConfiguration(expectedConfiguration, configuration);
        }

        [TestMethod]
        public void ShouldConfigureFromFileAndWatch()
        {
            // Arrange
            HmacConfigurationManager configurationManager = new HmacConfigurationManager();
            const string name = "TestConfiguration";
            const string originalAuthScheme = "TEST";
            const string modifiedAuthScheme = "TEST_MODIFIED";

            // Act
            configurationManager.ConfigureFromFileAndWatch("Hmac.config");
            IHmacConfiguration configuration = configurationManager.Get(name);
            using (StreamWriter writer = new StreamWriter("Hmac.config", false, Encoding.UTF8))
            {
                writer.Write(_hmacModifiedConfig);
                writer.Flush();
            }
            Thread.Sleep(1500);
            IHmacConfiguration modifiedConfiguration = configurationManager.Get(name);
            IHmacConfiguration defaultConfiguration = configurationManager.Get(configurationManager.DefaultConfigurationKey);
            using (StreamWriter writer = new StreamWriter("Hmac.config", false, Encoding.UTF8))
            {
                writer.Write(_hmacConfig);
                writer.Flush();
            }

            // Assert
            Assert.IsNotNull(configuration);
            Assert.AreEqual(name, configuration.Name);
            Assert.AreEqual(originalAuthScheme, configuration.AuthorizationScheme);
            Assert.IsNotNull(configuration);
            Assert.AreEqual(name, modifiedConfiguration.Name);
            Assert.AreEqual(modifiedAuthScheme, modifiedConfiguration.AuthorizationScheme);
            Assert.IsNotNull(defaultConfiguration);
            Assert.AreEqual(configurationManager.DefaultConfigurationKey, defaultConfiguration.Name);
        }

        [TestMethod]
        public void ShouldConfigureFromString()
        {
            // Arrange
            HmacConfigurationManager configurationManager = new HmacConfigurationManager();
            HmacConfiguration expectedConfiguration = new HmacConfiguration
            {
                Name = "TestConfiguration",
                UserHeaderName = "X-Test-User",
                AuthorizationScheme = "TEST",
                SignatureDataSeparator = "_",
                SignatureEncoding = "UTF-32",
                HmacAlgorithm = "HMACSHA256",
                MaxRequestAge = TimeSpan.FromMinutes(2),
                SignRequestUri = false,
                ValidateContentMd5 = true,
                Headers = new List<string> { "X-Test-Header-1", "X-Test-Header-2" }
            };
            
            // Act
            configurationManager.ConfigureFromString(_hmacConfig, HmacConfigurationFormat.Xml);
            IHmacConfiguration configuration = configurationManager.Get(expectedConfiguration.Name);

            // Assert
            AssertConfiguration(expectedConfiguration, configuration);
        }

        [TestMethod]
        public void ShouldGetDefaultConfiguration()
        {
            // Arrange
            HmacConfigurationManager configurationManager = new HmacConfigurationManager();
            HmacConfiguration expectedConfiguration = new HmacConfiguration
            {
                Name = configurationManager.DefaultConfigurationKey,
                UserHeaderName = "X-Auth-User",
                AuthorizationScheme = "HMAC",
                SignatureDataSeparator = "\n",
                SignatureEncoding = "UTF-8",
                HmacAlgorithm = "HMACSHA512",
                MaxRequestAge = TimeSpan.FromMinutes(5),
                SignRequestUri = true,
                ValidateContentMd5 = true,
                Headers = null
            };

            // Act
            IHmacConfiguration defaultConfigByKey = configurationManager.Get(configurationManager.DefaultConfigurationKey);
            IHmacConfiguration defaultConfig = configurationManager.GetDefault();

            // Assert
            AssertConfiguration(expectedConfiguration, defaultConfigByKey);
            AssertConfiguration(expectedConfiguration, defaultConfig);
        }

        [TestMethod]
        public void ShouldGetAllKeys()
        {
            // Arrange
            HmacConfigurationManager configurationManager = new HmacConfigurationManager();
            string[] expectedKeys =
            {
                "TestConfiguration",
                "TestConfiguration2"
            };

            // Act
            configurationManager.ConfigureFromString(_hmacModifiedConfig, HmacConfigurationFormat.Xml);
            ICollection<string> keys = configurationManager.GetAllKeys();

            // Assert
            Assert.IsNotNull(keys);
            Assert.IsTrue(expectedKeys.All(keys.Contains));
        }

        [TestMethod]
        public void ShouldTryGetConfiguration()
        {
            // Arrange
            HmacConfigurationManager configurationManager = new HmacConfigurationManager();
            const string existingConfigKey = "TestConfiguration";
            const string nonExistingConfigKey = "Wrong_TestConfiguration";

            // Act
            configurationManager.ConfigureFromString(_hmacModifiedConfig, HmacConfigurationFormat.Xml);

            IHmacConfiguration existingConfig;
            IHmacConfiguration nonExistingConfig;

            bool existingConfigFound = configurationManager.TryGet(existingConfigKey, out existingConfig);
            bool nonExistingConfigFound = configurationManager.TryGet(nonExistingConfigKey, out nonExistingConfig);

            // Assert
            Assert.IsNotNull(existingConfig);
            Assert.IsNull(nonExistingConfig);
            Assert.IsTrue(existingConfigFound);
            Assert.IsFalse(nonExistingConfigFound);
        }

        [TestMethod]
        public void ShouldContainsConfiguration()
        {
            // Arrange
            HmacConfigurationManager configurationManager = new HmacConfigurationManager();
            const string existingConfigKey = "TestConfiguration";
            const string nonExistingConfigKey = "Wrong_TestConfiguration";

            // Act
            configurationManager.ConfigureFromString(_hmacModifiedConfig, HmacConfigurationFormat.Xml);

            bool existingConfigFound = configurationManager.Contains(existingConfigKey);
            bool nonExistingConfigFound = configurationManager.Contains(nonExistingConfigKey);

            // Assert
            Assert.IsTrue(existingConfigFound);
            Assert.IsFalse(nonExistingConfigFound);
        }

        private void AssertConfiguration(IHmacConfiguration expected, IHmacConfiguration actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual);
                return;
            }

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.UserHeaderName, actual.UserHeaderName);
            Assert.AreEqual(expected.AuthorizationScheme, actual.AuthorizationScheme);
            Assert.AreEqual(expected.SignatureDataSeparator, actual.SignatureDataSeparator);
            Assert.AreEqual(expected.SignatureEncoding, actual.SignatureEncoding);
            Assert.AreEqual(expected.HmacAlgorithm, actual.HmacAlgorithm);
            Assert.AreEqual(expected.MaxRequestAge, actual.MaxRequestAge);
            Assert.AreEqual(expected.SignRequestUri, actual.SignRequestUri);

            if (expected.Headers != null)
            {
                Assert.IsNotNull(actual.Headers);
                Assert.IsTrue(expected.Headers.OrderBy(h => h).SequenceEqual(actual.Headers.OrderBy(h => h)));
            }
            else
            {
                Assert.IsNull(actual.Headers);
            }
        }
    }
}
