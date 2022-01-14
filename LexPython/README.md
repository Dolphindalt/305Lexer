Project for table driven lexical analyzers, written in Python, for the language SchemeExpress. 

In computer science, lexical analysis, lexing or tokenization is the process of converting a sequence of characters (such as in a computer program or web page) into a sequence of tokens (strings with an assigned and thus identified meaning). A program that performs lexical analysis may be termed a lexer, tokenizer, or scanner, though scanner is also a term for the first stage of a lexer. A lexer is generally combined with a parser, which together analyze the syntax of programming languages, web pages, and so forth.

You can use the callinput.sh script to call the default test cases.
```
bash callinput.sh [1-5]
```

If you want to call the program with custom source code, call this command:
```
python3 main.py lexicalTable.csv tokenTable.csv keywords.csv [source.*]
```