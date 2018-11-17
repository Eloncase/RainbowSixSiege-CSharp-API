# R6SApi

R6SApi is an easy-to-use asynchronous API for Rainbow Six Siege, written in c#, forked from [billy_yoyo](https://github.com/billy-yoyo/RainbowSixSiege-Python-API). To use it you'll need use your ubisoft email and password.

As of [351c229](https://github.com/Eloncase/RainbowSixSiege-CSharp-API/commit/351c229054d82ad6341a7d0f4632064f87097a72) library should parse current endpoints automatically. That means if there is no significant changes it should be working after R6 updates.

### Installation

Get it on [Nuget](https://www.nuget.org/packages/Eloncase.R6SiegeAPI/)

### Documentation

TBA

### Quick Example

```cs
using R6SiegeAPI;

static async Task MainAsync()
{
    var api = API.InitAPI(email, password, null);

    var player = await api.GetPlayer("Eloncase", Enums.Platform.UPLAY, Enums.UserSearchType.Name);
    var oper = await player.GetOperator("caveira");
    Console.WriteLine(oper.Kills);
}
```

### TODO

  - examples

### License

MIT


