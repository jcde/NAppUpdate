nuget pack src\NAppUpdate.Framework\NAppUpdate.Framework.csproj -Prop Configuration=Release -Build  

nuget push NAppUpdate.Framework*.nupkg -source http://nu/get -apikey chocolateyrocks
