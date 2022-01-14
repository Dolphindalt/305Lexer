"""
Main entry point for lexical analyzer program.
See the README.md for more information.
Author: Dalton Caron
"""
import sys
from parser import Parser
from lexer import Lexer

# Ensure the right amount of arguments are provided.
if len(sys.argv) != 5:
    print("python3 main.py [lexicalTable.csv] [tokenTable.csv] [keywords.csv] [sourceCode.*]")
    exit(0)

# Extract file names from the program arguments.
lexer_table_filename = sys.argv[1]
token_table_filename = sys.argv[2]
keywords_table_filename = sys.argv[3]
source_code_filename = sys.argv[4]

# Construct the parser with the given files.
parser = Parser(lexer_table_filename, token_table_filename, \
    source_code_filename, keywords_table_filename)

try:
    # Attempt to parse provided ipnut files.
    result_quad = parser.parse()
except ValueError as ve:
    # Failed to parse an input file. End the program.
    print(ve)
    exit(0)

# Construct the lexer from the parsed file outputs from the parser.
lexer = Lexer(result_quad[0], result_quad[1], result_quad[2], result_quad[3])
# Perform the lexical analysis.
token_stream = lexer.perform_analysis()
# Prompt the user to view the tokens.
input("Press enter to step: ")
for _, token in enumerate(token_stream):
    print(token)
    input()
print("Reached end of token stream. Ending program.")