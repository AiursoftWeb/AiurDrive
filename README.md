# AiurDrive

[![Build status](https://dev.azure.com/aiursoft/Star/_apis/build/status/AiurDrive%20Build)](https://dev.azure.com/aiursoft/Star/_build/latest?definitionId=15)

A file store for all kinds of users

## Try

Try a running AiurDrive [here](https://drive.aiursoft.com).

## Run in Azure

With the following ARM template you can automate the creation of the resources for this website.

[![Deploy to Azure](https://azuredeploy.net/deploybutton.svg)](https://deploy.azure.com/?repository=https://github.com/AiursoftWeb/AiurDrive/tree/master)

## Run in Ubuntu

First, specify a domain name for your Ubuntu 18.04+, brand new server.

And execute the following command in the server:

```bash
$ curl -sL https://github.com/AiursoftWeb/AiurDrive/raw/master/install.sh | sudo bash -s www.example.com
```

## Run locally

Requirements about how to run

* [.NET Core runtime 3.1.0 or later](https://github.com/dotnet/core/tree/master/release-notes)

Requirements about how to develop

* [.NET Core SDK 3.1.0 or later](https://github.com/dotnet/core/tree/master/release-notes)
* [VS Code](https://code.visualstudio.com) (Strongly suggest)

1. Excute `dotnet restore` to restore all dotnet requirements
2. Excute `dotnet run` to run the app
3. Use your browser to view [http://localhost:5000](http://localhost:5000)

## Run in Microsoft Visual Studio

1. Open the `.sln` file in the project path.
2. Press `F5`.

## Run in docker

Just install docker and docker-compose. Execute the following command.

```bash
$ docker build -t=aiurdrive .
$ docker run -d -p 8080:80 aiurdrive
```

That will start a web server at `http://localhost:8080` and you can test the app.

## How to contribute

There are many ways to contribute to the project: logging bugs, submitting pull requests, reporting issues, and creating suggestions.

Even if you have push rights on the repository, you should create a personal fork and create feature branches there when you need them. This keeps the main repository clean and your personal workflow cruft out of sight.

We're also interested in your feedback for the future of this project. You can submit a suggestion or feature request through the issue tracker. To make this process more effective, we're asking that these include more information to help define them more clearly.
