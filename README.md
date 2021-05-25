# AiurDrive

[![Build status](https://dev.azure.com/aiursoft/Star/_apis/build/status/AiurDrive%20Build)](https://dev.azure.com/aiursoft/Star/_build/latest?definitionId=15)
![Azure DevOps coverage](https://img.shields.io/azure-devops/coverage/aiursoft/Star/15)
![Website](https://img.shields.io/website?url=https%3A%2F%2Fdrive.aiursoft.com%2F%3Fshow%3Ddirect)

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

* [.NET Core runtime 5.0 or later](https://github.com/dotnet/core/tree/master/release-notes)

Requirements about how to develope

* [.NET Core SDK 5.0 or later](https://github.com/dotnet/core/tree/master/release-notes)
* [VS Code](https://code.visualstudio.com) (Strongly suggest)

1. Excute `dotnet restore` to restore all dotnet requirements
2. Excute `dotnet run` to run the app
3. Use your browser to view [http://localhost:5000](http://localhost:5000)

## Run in Microsoft Visual Studio

1. Open the `.sln` file in the project path.
2. Press `F5`.

## How to contribute

There are many ways to contribute to the project: logging bugs, submitting pull requests, reporting issues, and creating suggestions.

Even if you have push rights on the repository, you should create a personal fork and create feature branches there when you need them. This keeps the main repository clean and your personal workflow cruft out of sight.

We're also interested in your feedback for the future of this project. You can submit a suggestion or feature request through the issue tracker. To make this process more effective, we're asking that these include more information to help define them more clearly.
