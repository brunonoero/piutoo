using FeedWorker.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FeedWorker.Insightsentry
{
    /// <summary>
    /// https://insightsentry.com/demo/restapi
    /// </summary>
    public class InsightSentryClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public InsightSentryClient(string baseUrl, string apiKey)
        {
            _baseUrl = (baseUrl ?? throw new ArgumentNullException(nameof(baseUrl))).TrimEnd('/');
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Converte l'enum CandleInterval nel formato stringa dell'API
        /// </summary>
        private static string GetIntervalString(CandleInterval interval) => interval switch
        {
            CandleInterval.OneMinute => "1m",
            CandleInterval.FiveMinutes => "5m",
            CandleInterval.FifteenMinutes => "15m",
            CandleInterval.ThirtyMinutes => "30m",
            CandleInterval.OneHour => "1h",
            CandleInterval.FourHours => "4h",
            CandleInterval.OneDay => "1d",
            CandleInterval.OneWeek => "1w",
            _ => "1h"
        };

        /// <summary>
        /// Ottiene le candele storiche per un simbolo specifico
        /// </summary>
        /// <param name="symbol">Simbolo (es. FOREX:GC)</param>
        /// <param name="interval">Intervallo temporale delle candele</param>
        /// <param name="startDate">Data di inizio</param>
        /// <param name="endDate">Data di fine</param>
        /// <param name="useDeepHistory">Se true, usa l'endpoint deep per dati storici estesi</param>
        /// <param name="saveToFile">Percorso del file dove salvare il JSON (opzionale)</param>
        /// <returns>Lista di candele</returns>
        public async Task<List<Candle>> GetCandlesAsync(
            string symbol,
            CandleInterval interval,
            DateTime startDate,
            DateTime endDate,
            bool useDeepHistory = false,
            string? saveToFile = null)
        {
            var intervalStr = GetIntervalString(interval);
            var startStr = startDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            var endStr = endDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

            var endpoint = useDeepHistory ? "history/deep" : "history";
            var encodedSymbol = Uri.EscapeDataString(symbol);

            var url = $"{_baseUrl}/symbols/{encodedSymbol}/{endpoint}?interval={intervalStr}&start={startStr}&end={endStr}";

            Console.WriteLine($"Chiamata API: {url}");

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Errore API: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Deserializza la risposta
            var candleResponse = JsonSerializer.Deserialize<CandleResponse>(content, options);

            if (candleResponse?.Series == null)
            {
                return new List<Candle>();
            }

            Console.WriteLine($"Simbolo: {candleResponse.Code}, Tipo: {candleResponse.BarType}");

            // Salva il DTO con DateTime leggibile su file se richiesto
            if (!string.IsNullOrEmpty(saveToFile))
            {
                // Converti in DTO con DateTime leggibili
                var responseDto = new CandleResponseDto
                {
                    Symbol = candleResponse.Code,
                    BarType = candleResponse.BarType,
                    BarEnd = candleResponse.BarEnd.HasValue
                        ? DateTimeOffset.FromUnixTimeSeconds((long)candleResponse.BarEnd.Value).UtcDateTime
                        : null,
                    LastUpdate = candleResponse.LastUpdate.HasValue
                        ? DateTimeOffset.FromUnixTimeMilliseconds(candleResponse.LastUpdate.Value).UtcDateTime
                        : null,
                    CandleCount = candleResponse.Series.Count,
                    Candles = candleResponse.Series.Select(c => c.ToDto()).ToList()
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var formattedJson = JsonSerializer.Serialize(responseDto, jsonOptions);
                await File.WriteAllTextAsync(saveToFile, formattedJson);
                Console.WriteLine($"JSON salvato in: {saveToFile}");
            }

            return candleResponse.Series;
        }

        /// <summary>
        /// Converte l'enum CandleInterval in bar_type e bar_interval per l'API v3
        /// </summary>
        private static (string barType, int barInterval) GetBarTypeAndInterval(CandleInterval interval) => interval switch
        {
            CandleInterval.OneMinute => ("minute", 1),
            CandleInterval.FiveMinutes => ("minute", 5),
            CandleInterval.FifteenMinutes => ("minute", 15),
            CandleInterval.ThirtyMinutes => ("minute", 30),
            CandleInterval.OneHour => ("hour", 1),
            CandleInterval.FourHours => ("hour", 4),
            CandleInterval.OneDay => ("day", 1),
            CandleInterval.OneWeek => ("week", 1),
            _ => ("hour", 1)
        };

        /// <summary>
        /// Ottiene le candele realtime per un simbolo specifico usando l'API v3
        /// </summary>
        /// <param name="symbol">Simbolo (es. COMEX:GC1!)</param>
        /// <param name="interval">Intervallo temporale delle candele</param>
        /// <param name="limit">Numero di candele da recuperare (dp parameter)</param>
        /// <returns>Lista di candele</returns>
        public async Task<List<Candle>> GetRealtimeCandlesAsync(
            string symbol,
            CandleInterval interval,
            int limit)
        {
            var (barType, barInterval) = GetBarTypeAndInterval(interval);
            var encodedSymbol = Uri.EscapeDataString(symbol);

            // Costruisce l'URL secondo la documentazione API v3
            // https://api.insightsentry.com/v3/symbols/{symbol}/series?bar_type={type}&bar_interval={interval}&extended=true&dadj=false&badj=true&dp={limit}&long_poll=false&settlement=true
            var url = $"{_baseUrl}/symbols/{encodedSymbol}/series?bar_type={barType}&bar_interval={barInterval}&extended=true&dadj=false&badj=true&dp={limit}&long_poll=false&settlement=true";

            Console.WriteLine($"[GetRealtimeCandlesAsync] BaseUrl: {_baseUrl}");
            Console.WriteLine($"[GetRealtimeCandlesAsync] Simbolo: {symbol}, Encoded: {encodedSymbol}");
            Console.WriteLine($"[GetRealtimeCandlesAsync] BarType: {barType}, BarInterval: {barInterval}, Depth: {limit}");
            Console.WriteLine($"[GetRealtimeCandlesAsync] URL completo: {url}");

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorMessage = $"Errore API: {response.StatusCode} - {errorContent}. URL chiamato: {url}";
                Console.WriteLine($"[GetRealtimeCandlesAsync] {errorMessage}");
                throw new HttpRequestException(errorMessage);
            }

            var content = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Deserializza la risposta
            var candleResponse = JsonSerializer.Deserialize<CandleResponse>(content, options);

            if (candleResponse?.Series == null)
            {
                return new List<Candle>();
            }

            Console.WriteLine($"[GetRealtimeCandlesAsync] Risposta ricevuta - Simbolo: {candleResponse.Code}, Tipo: {candleResponse.BarType}, Candele: {candleResponse.Series.Count}");

            return candleResponse.Series;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
