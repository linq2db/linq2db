# ActivityService (Metrics)

## Overview

The ActivityService provides functionality to collect critical LinqToDB telemetry data, that can be used to monitor, analyze, and optimize your application.

Linq2DB provides simple and easy to use API to collect metrics data.

## IActivity interface

The `IActivity` represents a single activity that can be measured. 

## ActivityBase class

The `ActivityBase` class provides a basic implementation of the `IActivity` interface. You do not have to use this class.
However, it can help you to avoid incompatibility issues in the future if the `IActivity` interface is extended.

## ActivityService class

The `ActivityService` class provides a simple API to register factory methods that create `IActivity` instances or `null` for provided `ActivityID` event.

## ActivityID

The `ActivityID` is a unique identifier of the LinqToDB activity. It is used to identify the activity in the metrics data.

