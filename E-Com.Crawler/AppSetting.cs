using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Com.Crawler
{
    public class AppSetting
    {
        public string AzureWebJobsStorage { get; private set; }
        public bool IsAnalysisMode { get; private set; }
        public string ContainerName { get; private set; }
        public List<string> ParsingStrategy1EcomUrls { get; private set; }
        public List<string> ParsingStrategy2EcomUrls { get; private set; }
        public int ThresholdTitleLength { get; private set; }
        public bool ProductUrlContainsSegments { get; private set; }

        public AppSetting()
        {
            try
            {
                AzureWebJobsStorage = getStringFromEnv("AzureWebJobsStorage");
                IsAnalysisMode = getBoolFromEnv("IsAnalysisMode");
                ContainerName = getStringFromEnv("ContainerName");
                ParsingStrategy1EcomUrls = getListFromEnv("ParsingStrategy1EcomUrls");
                ParsingStrategy2EcomUrls = getListFromEnv("ParsingStrategy2EcomUrls");
                ThresholdTitleLength = getIntFromEnv("ThresholdTitleLength");
                ProductUrlContainsSegments = getBoolFromEnv("ProductUrlContainsSegments");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(1);
            }
        }
        private string getEnv(string key)
        {
            return Environment.GetEnvironmentVariable(key);
        }

        private bool getBoolFromEnv(string key)
        {
            string envValue = getEnv(key);

            if (envValue == null)
            {
                throw new Exception($"Environment variable '{key}' is not set.");
            }
            bool _isAnalysisMode;
            if (!bool.TryParse(envValue, out _isAnalysisMode))
            {
                throw new Exception($"Failed to parse the value '{envValue}' for '{key}'. Expected a boolean value.");
            }
            return _isAnalysisMode;
        }
        private string getStringFromEnv(string key)
        {

            string envValue = getEnv(key);

            if (string.IsNullOrWhiteSpace(envValue))
            {
                throw new ArgumentException($"'{key}' is not set or whitespace.");
            }
            return envValue;
        }
        private List<string> getListFromEnv(string key)
        {

            string envValue = getEnv(key);

            if (string.IsNullOrWhiteSpace(envValue))
            {
                throw new ArgumentException($"'{key}' is not set or whitespace.");
            }
            return envValue.Split(";").ToList();
        }
        private int getIntFromEnv(string key)
        {
            string envValue = getEnv(key);


            if (envValue == null)
            {
                throw new Exception($"Environment variable '{key}' is not set.");

            }
            int _thresholdTitleLength;
            if (!int.TryParse(envValue, out _thresholdTitleLength))
            {
                throw new Exception($"Failed to parse the value '{envValue}' for '{key}'. Expected a int value.");
            }
            return _thresholdTitleLength;
        }
    }
}
