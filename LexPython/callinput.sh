#!/bin/bash
selection=$1
if [ ! $1 ] || [ $1 -gt 5 ] || [ $1 -lt 1 ]
then
    echo "Please given a valid test case number."
else
    python3 main.py lexicalTable.csv tokenTable.csv keywords.csv test${selection}.txt
fi