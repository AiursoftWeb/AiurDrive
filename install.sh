aiur() { arg="$( cut -d ' ' -f 2- <<< "$@" )" && curl -sL https://gitlab.aiursoft.cn/aiursoft/aiurscript/-/raw/master/$1.sh | sudo bash -s $arg; }
aiurdrive_path="/opt/apps/AiurDrive"

install_aiurdrive()
{
    port=$(aiur network/get_port) && echo "Using internal port: $port"
    aiur network/enable_bbr
    aiur system/set_aspnet_prod
    aiur install/caddy
    aiur install/dotnet
    aiur git/clone_to https://gitlab.aiursoft.com/aiursoft/aiurdrive ./AiurDrive
    aiur dotnet/publish $aiurdrive_path ./AiurDrive/src/AiurDrive.csproj
    aiur services/register_aspnet_service "aiurdrive" $port $aiurdrive_path "AiurDrive"
    aiur caddy/add_proxy $1 $port

    echo "Successfully installed AiurDrive as a service in your machine! Please open https://$1 to try it now!"
    rm ./AiurDrive -rf
}

# Example: install_aiurdrive http://aiurdrive.local
install_aiurdrive "$@"
