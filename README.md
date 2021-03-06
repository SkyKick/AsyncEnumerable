## SUMMARY

Introduces `IAsyncEnumerable`, `IAsyncEnumerator`, and `ForEachAsync()`


## PROBLEM SPACE

Helps to (a) create an element provider, where producing an element can take a lot of time
due to dependency on other asynchronous events (e.g. wait handles, network streams), and
(b) a consumer that processes those element as soon as they are ready without blocking
the thread (the processing is scheduled on a worker thread instead).


## EXAMPLE 1 (demonstrates usage only)

    using System.Collections.Async;

    static IAsyncEnumerable<int> ProduceAsyncNumbers(int start, int end)
    {
      return new AsyncEnumerable<int>(async yield => {

        // Just to show that ReturnAsync can be used multiple times
        await yield.ReturnAsync(start);

        for (int number = start + 1; number <= end; number++)
          await yield.ReturnAsync(number);

        // You can break the enumeration loop with the following call:
        yield.Break();

        // This won't be executed due to the loop break above
        await yield.ReturnAsync(12345);
      });
    }

    // Just to compare with synchronous version of enumerator
    static IEnumerable<int> ProduceNumbers(int start, int end)
    {
      yield return start;

      for (int number = start + 1; number <= end; number++)
        yield return number;

      yield break;

      yield return 12345;
    }

    static async Task ConsumeNumbersAsync()
    {
      var asyncEnumerableCollection = ProduceAsyncNumbers(start: 1, end: 10);
      await asyncEnumerableCollection.ForEachAsync(async number => {
        await Console.Out.WriteLineAsync($"{number}");
      });
    }

    // Just to compare with synchronous version of enumeration
    static void ConsumeNumbers()
    {
      // NOTE: IAsyncEnumerable is derived from IEnumerable, so you can use either
      var enumerableCollection = ProduceAsyncNumbers(start: 1, end: 10);
      //var enumerableCollection = ProduceNumbers(start: 1, end: 10);

      foreach (var number in enumerableCollection) {
        Console.Out.WriteLine($"{number}");
      }
    }


## EXAMPLE 2 (real scenario, pseudo code)

    using System.Collections.Async;

    static IAsyncEnumerable<KeyValuePair<string, string>> ReadRemoteSettings(Uri resourceUri)
    {
      return new AsyncEnumerable<KeyValuePair<string, string>>(async yield => {
        using (var client = new HttpClient()) {

          client.BaseAddress = resourceUri;

          using (var request = new HttpRequestMessage(HttpMethod.Get, resourceUri)) {

            using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, yield.CancellationToken)) {

              if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception($"The server returned: {response.ReasonPhrase}");

              using (var stream = await response.Content.ReadAsStreamAsync()) {

                var xmlSettings = new XmlReaderSettings() { IgnoreComments = true, IgnoreWhitespace = true };

                using (var xmlReader = XmlReader.Create(stream, xmlSettings)) {

                  if (!await xmlReader.ReadAsync())
                    yield.Break();

                  if (xmlReader.NodeType == XmlNodeType.XmlDeclaration)
                    await xmlReader.SkipAsync();

                  if (xmlReader.NodeType != XmlNodeType.Element && xmlReader.LocalName != "Settings")
                    yield.Break();

                  while (await xmlReader.ReadAsync()) {
                    if (xmlReader.NodeType == XmlNodeType.Element && !xmlReader.IsEmptyElement) {
                      var settingName = xmlReader.LocalName;
                      var settingValue = xmlReader.Value;
                      await yield.ReturnAsync(new KeyValuePair<string, string>(settingName, settingValue));
                    }
                  }
                }
              }
            }
          }
        }
      });
    }

    static async Task FetchAndPrintSettingsAsync()
    {
      var resourceUri = new Uri("http://localhost:12345/Settings.XML");
      var timeout = TimeSpan.FromSeconds(30);
      var cts = new CancellationTokenSource(timeout);
      var settingCollection = ReadRemoteSettings(resourceUri);

      await settingCollection.ForEachAsync(async setting => {
        await Console.Out.WriteLineAsync($"{setting.Key} = {setting.Value}");
      },
      cancellationToken: cts.Token);
    }


## EXAMPLE 3 (async convert)

    IAsyncEnumerable<Bar> ConvertFoosToBars(IAsyncEnumerable<Foo> items)
    {
        return new AsyncEnumerable<Bar>(async yield => {
            await items.ForEachAsync(async foo => {
                var bar = foo.ToBar();
                await yield.ReturnAsync(bar);
            });
        });
    }


## EXAMPLE 4 (async parallel for-each)

    async Task<IReadOnlyCollection<string>> GetStringsAsync(IEnumerable<T> uris, HttpClient httpClient, CancellationToken cancellationToken)
    {
        var result = new ConcurrentBag<string>();
        
        await uris.ParallelForEachAsync(
            async uri =>
            {
                var str = await httpClient.GetStringAsync(uri, cancellationToken);
                result.Add(str);
            },
            maxDegreeOfParallelism: 5,
            cancellationToken);
        
        return result;
    }

## WILL THIS MAKE MY APP FASTER?

No and Yes. Just making everything `async` makes your app tiny little bit slower because it
adds overhead in form of state machines and tasks. However, this will help you to better
utilize worker threads in the app because you don't need to block them anymore by waiting
on the next element to be produced - i.e. this will make your app better in general when it
has such multiple enumerations running in parallel. The best fit for `IAsyncEnumerable` is a
case when you read elements from a network stream, like HTTP + XML (as shown above; SOAP),
or a database client implementation where result of a query is a set or rows.


## REFERENCES

GitHub: https://github.com/tyrotoxin/AsyncEnumerable
NuGet.org: https://www.nuget.org/packages/AsyncEnumerator/
License: https://opensource.org/licenses/MIT