build:
	# @csharpier format ./ --config-path .csharpierrc	
	# @dotnet format style
	@dotnet clean
	@dotnet restore
	@dotnet build -c Debug
	@sudo rm -r ~/Documents/boxy-online/bruna/jellyfin/config/plugins/m3utunner
	@sudo cp -r ./Jellyfin.Plugin.Template/bin/Debug/net8.0/ ~/Documents/boxy-online/bruna/jellyfin/config/plugins/m3utunner
