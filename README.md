[![Community Extension](https://img.shields.io/badge/Community%20Extension-An%20open%20source%20community%20maintained%20project-FF4700)](https://github.com/camunda-community-hub/community)
[![Lifecycle: Incubating](https://img.shields.io/badge/Lifecycle-Incubating-blue)](https://github.com/Camunda-Community-Hub/community/blob/main/extension-lifecycle.md#incubating-)
![Compatible with: Camunda Platform 7](https://img.shields.io/badge/Compatible%20with-Camunda%20Platform%207-26d07c)
![Compatible with: Camunda Platform 8](https://img.shields.io/badge/Compatible%20with-Camunda%20Platform%208-0072Ce)
![Educational Tooling](https://img.shields.io/badge/Educational%20Tooling-Project%20for%20getting%20started%20with%20Camunda%20for%20educators-%239F2B68)
![Maven Central](https://img.shields.io/nexus/r/https/s01.oss.sonatype.org/io.github.pme123/camunda-dmn-tester-shared_2.13.svg)
![GitHub repo size](https://img.shields.io/github/repo-size/pme123/camunda-dmn-tester)
![Docker Pulls](https://img.shields.io/docker/pulls/pame/camunda-dmn-tester)

# Camunda DMN Table Tester
A little DMN Table tester with the following Goals:
* As a developer I want to test the DMNs that I get from the Business, even not knowing the concrete rules.
* Business people can create their own tests.
* They can easily adjust the tests to the dynamic nature of DMN Tables.

> This is exactly the kind of tool that as soon as I saw it, I knew it was something I was missing.
>
> I really enjoy being able to help people especially new people with modeling DMN, so I often have people sending me models to validate.
> 
> This tool's fundamental helpfulness comes first from being able to quickly catch some of the simple common errors, but also in the way it makes it easy for business users to explore the wide variety of possible ways in DMN that decisions can be modeled. This was in fact the original goal of DMN and this is a strong step towards narrowing the Business IT Gap for the standard.
>  
> **Niall Deehan** Developer Relations Camunda

## Version
I work at a redesign at the moment and published it > Version `> 0.15.*`.

Not all features are ready in the UI (mostly the test cases). 
So if you need them stay on `0.14.0`.

## Usage
I wrote two blog article that explains how you can use it:

* [Testing (Camunda)-DMN Tables automatically](https://pme123.medium.com/testing-camunda-dmn-tables-automatically-713497ab57e6)

* [Use the DMN Tester for Continuous Integration CI](https://pme123.medium.com/testing-camunda-dmn-tables-automatically-part-2-d3931ed38f51)

And there is a Recording from Camunda Community Summit:
* [Testing DMN Tables Automatically](https://page.camunda.com/ccs-mengelt-testing-dmn-tables-automatically) 

## Test Configuration
![Config Dialog](docs/Screenshot_configDialog.png)
**1 Test Mode**
  
  You can configure a DMN Table Test as:

  - a Unit test (the inputs are not created from evaluating the depending decisions - you define them directly)
  - with all its dependent Inputs (decisions like DMN Tables or Literal Expressions).

**2 Decision Id**

  The id of your table - be aware this is not the name
  - you only find it in the 'Edit DRD' Panel with the DMN Table Object selected.

**3 Path of the DMN**
  
  This path depends on your Docker Image (check _runDmnTester.sh_).
  
  Example: `-v $(pwd)/dmns:/opt/docker/dmns \` 
  - This assumes your DMNs are in a folder _dmns_.
  - So your Path would be `dmns/MyDmn.dmn`.

**4 Test Inputs**

  Add the following for each Input:
  - Key: This is what you need in your input expressions.
  - Type: The Type specified.
  - Values: A comma separated list of all possible inputs.
  - Null Value: If it is possible that the input may be _null_ (not required), check this box.

**5 Test Variables** 

  You can define Variables you use in your Rules. Usually one value is enough.

## Technologies
This projects builds on cool Open Source Projects. So my thanks go to:

### Shared
* [Circe](https://circe.github.io/circe/):
  > Library to serialize and deserialize Json to communicate between the client and the server. 
  > The library works on both Scala and Scala.js.

### Client
* [Laminar](https://laminar.dev)
  > Native Scala.js library for building user interfaces
* [Laminar bindings for SAP ui5](https://github.com/sherpal/LaminarSAPUI5Bindings)
  > Bindings that allows using Laminar with a decent Webcomponents library.

### Server
* [Scala DMN](https://github.com/camunda/dmn-scala)
  > An engine to execute decisions according to the DMN 1.1 specification.
* [http4s](https://http4s.org)
  > Typeful, functional, streaming HTTP for Scala.
* [ZIO Config](https://zio.github.io/zio-config/)
  > A functional, composable ZIO interface to configuration
* [ZIO](https://zio.dev)
  > Type-safe, composable asynchronous and concurrent programming for Scala

### Start Script
* [Ammonite](https://ammonite.io/#Ammonite)
  > Ammonite lets you use the Scala language for scripting purposes: in the REPL, as scripts, as a library to use in existing projects, or as a standalone systems shell.
## Development
### Server
`sbt server/run`

This starts the Web Server on **Port 8883**.

>This copies the client assets to the classpath of the server.
> So make sure you run `npm run build` before.
>
> Or use the client as described in the next chapter.

### Client
In the sbt console: `~fastLinkJS`

This creates the Javascript file from the Scala classes on the fly with every change.

In the terminal: `npm run dev`

This will watch all your changes in the client and automatically refresh your Browser Session.
This uses [Vite](https://vitejs.dev/guide/).

Open in the Browser **http://localhost:5173**.

## Releasing
Just run `amm ./publish-release.sc VERSION`.

Due to problems with the `"org.xerial.sbt" % "sbt-sonatype"` Plugin you have to release manually:
- https://s01.oss.sonatype.org/#stagingRepositories
  - login
  - check Staging Repository
  - hit _close_ Button
  - hit _release_ Button
    
> if you do not see any of the buttons or repository hit the _refresh_ Button.

### Local publish
Just run `amm ./publish-release.sc VERSION-SNAPSHOT`.

For now as soon as you publish a SNAPSHOT - it is always published locally.

### Docker
There are 2 Docker Images:

1. The DMN Tester App:

   `sbt server/docker:publishLocal` creates a Docker Image - see also next chapter.

2. The Unit Test Creator:
   See its [README.md](docker/README.md)
   
## Try it out
There are Docker Images you can use with an example in the `demo` directory.

`cd demo`

See the according [README](demo/README.md)

# DMN Tester (.NET - Clean Architecture)

## Cấu trúc thư mục

```
src/
  DmnTester.Domain/         # Entities, Interfaces domain
  DmnTester.Application/    # UseCases, Application logic
  DmnTester.Infrastructure/ # Implement service, Data access
  DmnTester.Presentation/   # API Controllers, DTO, Program.cs
```

## Cách build & chạy

### 1. Build toàn bộ solution
```bash
cd src
 dotnet build
```

### 2. Chạy API
```bash
cd src/DmnTester.Presentation
 dotnet run
```

- API mặc định chạy ở: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger

## Các endpoint chính
- `GET /api/dmn/configs` — Lấy danh sách configs
- `GET /api/dmn/configs/{decisionId}` — Lấy config theo ID
- `POST /api/dmn/evaluate` — Đánh giá DMN
- `POST /api/dmn/test` — Chạy test DMN

## Clean Architecture
- **Domain**: Chỉ chứa business entity, interface thuần
- **Application**: Chỉ chứa usecase, không phụ thuộc Infrastructure
- **Infrastructure**: Chứa implement service, data, external
- **Presentation**: Chỉ chứa controller, DTO, không chứa logic nghiệp vụ

## Debug nhanh
- Có sẵn file `launchSettings.json` để debug bằng Visual Studio hoặc Rider

---

Nếu có vấn đề về cấu hình hoặc chạy, hãy kiểm tra lại các file cấu hình và đảm bảo .NET 8.0 SDK đã được cài đặt.

# DMN Tester UI

Angular frontend for testing DMN files with the .NET backend API.

## Features

- **Generate Tests**: Generate test cases from DMN files
- **Evaluate Decisions**: Test individual decisions with custom inputs
- **Batch Testing**: Run multiple test cases in batch
- **Convert Tests**: Convert generated tests to batch test format

## Prerequisites

- Node.js (version 18 or higher)
- Angular CLI
- .NET 8.0 backend running on port 5001

## Installation

1. Install dependencies:
```bash
npm install
```

2. Start the development server:
```bash
npm start
```

The application will be available at `http://localhost:4200`.

## Usage

1. **Generate Tests**: Enter a DMN file name and click "Generate Tests" to create test cases
2. **Evaluate Decision**: Test a single decision with custom inputs
3. **Batch Test**: Run multiple test cases by providing JSON data
4. **Convert to Batch**: Convert generated tests to batch test format

## API Endpoints

The UI communicates with the following backend endpoints:

- `POST /api/dmn/generate-test` - Generate test cases from DMN
- `POST /api/dmn/evaluate` - Evaluate a single decision
- `POST /api/dmn/batch-test` - Run batch tests
- `POST /api/dmn/convert-generate-to-batch` - Convert generated tests to batch format

## Development

- Run tests: `npm test`
- Build for production: `npm run build`
- Watch mode: `npm run watch`

## Configuration

Update the API base URL in `src/app/services/api.service.ts` if needed:

```typescript
private baseUrl = 'http://localhost:5001/api/dmn';
```
