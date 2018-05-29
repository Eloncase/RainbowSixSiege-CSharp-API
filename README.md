# r6sapi

r6sapi is an easy-to-use asynchronous API for rainbow six siege, written in c#, forked from [billy_yoyo](https://github.com/billy-yoyo/RainbowSixSiege-Python-API). To use it you'll need use your ubisoft email and password

### Installation

TBA

### Documentation

TBA

### Quick Example

```cs
static async Task MainAsync()
{
    var api = R6SiegeAPI.API.InitAPI(email, password, null);

    var player = await api.GetPlayer("Eloncase", R6SiegeAPI.Enums.Platform.UPLAY, R6SiegeAPI.Enums.UserSearchType.Name);
    var oper = player.GetOperator("caveira");
    Console.WriteLine(oper.Kills)
}
```

### TODO

  - examples

### License


MIT


