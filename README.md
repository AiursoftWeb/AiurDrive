# AiurDrive

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](https://gitlab.aiursoft.cn/aiursoft/aiurdrive/-/blob/master/LICENSE)
[![Pipeline stat](https://gitlab.aiursoft.cn/aiursoft/aiurdrive/badges/master/pipeline.svg)](https://gitlab.aiursoft.cn/aiursoft/aiurdrive/-/pipelines)
[![Test Coverage](https://gitlab.aiursoft.cn/aiursoft/aiurdrive/badges/master/coverage.svg)](https://gitlab.aiursoft.cn/aiursoft/aiurdrive/-/pipelines)
[![ManHours](https://manhours.aiursoft.cn/r/gitlab.aiursoft.cn/aiursoft/aiurdrive.svg)](https://gitlab.aiursoft.cn/aiursoft/aiurdrive/-/commits/master?ref_type=heads)
[![Website](https://img.shields.io/website?url=https%3A%2F%2Fdrive.aiursoft.com%2F%3Fshow%3Ddirect)](https://drive.aiursoft.com)

A file store for all kinds of users

## Try

Try a running AiurDrive [here](https://drive.aiursoft.com).

## Run in Ubuntu

First, specify a domain name for your Ubuntu 18.04+, brand-new server.

And execute the following command in the server:

```bash
curl -sL https://gitlab.aiursoft.cn/aiursoft/aiurdrive/-/raw/master/install.sh | sudo bash -s http://aiurdrive.local
```

## Run locally

Requirements about how to run

1. [.NET 7 SDK](http://dot.net/)
2. Execute `dotnet run` to run the app
3. Use your browser to view [http://localhost:5000](http://localhost:5000)

## Run in Microsoft Visual Studio

1. Open the `.sln` file in the project path.
2. Press `F5`.

## How to contribute

There are many ways to contribute to the project: logging bugs, submitting pull requests, reporting issues, and creating suggestions.

Even if you with push rights on the repository, you should create a personal fork and create feature branches there when you need them. This keeps the main repository clean and your workflow cruft out of sight.

We're also interested in your feedback on the future of this project. You can submit a suggestion or feature request through the issue tracker. To make this process more effective, we're asking that these include more information to help define them more clearly.
