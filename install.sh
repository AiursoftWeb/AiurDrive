aiur() { arg="$( cut -d ' ' -f 2- <<< "$@" )" && curl -sL https://github.com/AiursoftWeb/AiurScript/raw/master/$1.sh | sudo bash -s $arg; }
aiurdrive_path="/opt/apps/AiurDrive"

install_aiurdrive()
{
    if [[ $(curl -sL ifconfig.me) == "$(dig +short $1)" ]]; 
    then
        echo "IP is correct."
    else
        echo "$1 is not your current machine IP!"
        return 9
    fi

    port=$(aiur network/get_port) && echo "Using internal port: $port"
    aiur network/enable_bbr
    aiur system/set_aspnet_prod
    aiur install/caddy
    aiur install/dotnet
    aiur git/clone_to AiursoftWeb/AiurDrive ./AiurDrive
    aiur dotnet/publish $aiurdrive_path ./AiurDrive/src/AiurDrive.csproj
    aiur services/register_aspnet_service "aiurdrive" $port $aiurdrive_path "AiurDrive"
    aiur caddy/add_proxy $1 $port
    aiur firewall/enable_firewall
    aiur firewall/open_port 443
    aiur firewall/open_port 80

    echo "Successfully installed AiurDrive as a service in your machine! Please open https://$1 to try it now!"
    rm ./AiurDrive -rf
}

install_aiurdrive "$@"
