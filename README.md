# CareerHub API - Assignment 1.1

## Overview

A .NET 10 Web API built to serve as the backend foundation for the CareerHub platform, utilizing `record` types for clean domain modeling.

## Running the Project

1. Clone the repository.
2. Run `dotnet run` in the terminal.
3. Navigate to `http://localhost:<port>/scalar` to view the interactive Scalar UI and test the endpoints directly in the browser.

## Architectural Choice

I opted to use **Controllers** instead of Minimal APIs for this assignment. Controllers provide a structured, attribute-routed foundation that scales well as the project grows throughout the training programme. The endpoints use `async/await` to prevent thread starvation, and the dummy data is safely abstracted into an injectable `JobStore` service.
