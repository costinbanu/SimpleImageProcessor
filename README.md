# SimpleImageProcessor
Straightforward license plate bluring and image resizing tool.

The license plate detecting logic is provided by [openAlpr](https://www.openalpr.com/) (repo at https://github.com/openalpr/openalpr).

The bluring logic is based off of https://gist.github.com/superic/8165746.

The interface is in Romanian. Take contact for translations, though it is mostly disclaimers and a few instructions that are straight-forward :)

## API
The project also exposes an API endpoint that can be used for automatic image processing. Integration with the endpoint is done via http(s) request:
```
POST api/process-image
Body (multipart/form-data):

File = multipart form file with complete content type and content disposition headers
HideLicensePlates = boolean, whether plates should be hidden or not
SizeLimit = number, desired maximum image size, in MB. Null if no resize is desired.
```

Sample C# integration with the api: see `OnPostAddAttachment` method [here](https://github.com/costinbanu/PhpbbInDotnet/blob/metrouusor/PhpbbInDotnet.Forum/Pages/PostingHandlers.cs).

The API is secured by API key (request header key `X-API-Key`), which should probably be upgraded to something stronger.


### App settings
Sample `appsettings.json` contents:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*",
  "ApiKey": " ... your own api key ... "
}
```