#!/usr/bin/env bash

set -e

for dir in ./Components/*Test
do
    dotnet test ${dir}/*.csproj;
done

for dir in ./Applications/*
do
    dotnet build ${dir};
done

dotnet test IntegrationTest/*.csproj
