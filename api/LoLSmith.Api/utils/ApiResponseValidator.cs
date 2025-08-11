namespace Utils
{
    public static class ApiResponseValidator
    {
        public static void VerifyStatusCode(HttpResponseMessage res)
        {
            if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new KeyNotFoundException("Summoner not found");
            }
            else if (res.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                    res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException("Invalid or Expired Riot API key");
            }
            else if (res.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                throw new InvalidOperationException("Rate limited by Riot API");
            }
            else
            {
                res.EnsureSuccessStatusCode();
            }
        }
    }
}