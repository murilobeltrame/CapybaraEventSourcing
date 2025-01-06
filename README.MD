# Capybara Event Sourcing Demo

## Purpose

This demo showcases an event-sourcing implementation using a fictional story about capybaras. 
The purpose is to illustrate how event sourcing can be used to track changes over time in a system.
We`re going to highlight the main features such as:
1. Complete history of changes;
2. Audit and compliance;
3. Scalability and performance;
4. Replay and debugging;
5. Flexibility;
6. Integration with reactive systems;

## The Capybara Story

### 1. Complete history of changes;

Cléo writes everything in a diary: who arrived, who left, and who brought food. 
Each note is an event recorded with date and time. 
If someone asks how the party was 3 months ago, Cléo can recount everything in minute detail.

### 2. Audit and compliance;

One day, the chief capybara wants to know who ate all the carrots. 
Cléo reviews the records and identifies the culprits (it was Capybara Carlos!). 
No fights, just facts.

### 3. Scalability and Performance

CapybaraVille grows, and Cléo starts recording events in several notebooks organized by sectors. 
Other capybaras help with the process, noting events in parallel and synchronizing everything at the end of the day.

### 4. Replay and Debugging

During the fruit fair, a problem occurs: the stock disappears! 
Cléo opens the records and replays the events to find out what went wrong.
It was a counting error in the "Watermelon Delivery" event.

### 5. Flexibility for New Requirements

The village decides to start recording the quantity of fruits, something that wasn't done before. 
Cléo doesn't alter the old records—she just adds future events with this new information.

### 6. Integration with Reactive Systems

Now the village wants automatic alerts when an important event happens—like the arrival of a carrot truck. 
Cléo adds a bell that rings whenever she notes something relevant.

## Running the Application

To run the application, follow these steps:

```sh
dotnet build
dotnet run --project CapybaraVille.AppHost
```

## Dependencies

This is an .NET Aspire application. You will need:
* [.NET SDK 9+](https://dotnet.microsoft.com/download)
* An [Aspire compatible container runtime](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling#container-runtime)