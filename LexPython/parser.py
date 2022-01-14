"""
The parser file contains the class  responsible for validating and parsing the 
input files.
Author: Dalton Caron
"""
import os

class Parser:
    """
    A class responsible for validating and parsing the provided inputs.
    """
    def __init__(self, lexical_table_filename, token_table_filename, \
        source_filename, keywords_table_filename):
        """
        Constructor that stores file names and validates them. May raise value
        error if the validation step fails.
        @param lexical_table_filename: A string file name for the lexical table.
        @param token_table_filename: A string file name for the lexical table.
        @param source_filename: A string file name for the source code of the
        program.
        @param keywords_table_filename A string file name for the keywords table.
        """
        self.lexical_table_filename = lexical_table_filename
        self.token_table_filename = token_table_filename
        self.source_filename = source_filename
        self.keywords_table_filename = keywords_table_filename
        self.__validate()

    def parse(self):
        """
        Performs the parsing of the provided input. Is meant to be called by
        the user.
        @return: A quad (lexical table map, token table map, source code string,
            keyword list).
        """
        return (self.__parse_lexical_table(), self.__parse_token_table(), \
            self.__parse_source_code(), self.__parse_keywords())

    def __parse_lexical_table(self):
        """
        Parses the lexical table from the file given in the constructor.
        @return: A lexical table map.
        """
        table_string = self.__read_file_as_string(self.lexical_table_filename)
        rows = table_string.split('\n')
        line1data = rows[0].rstrip().split(',')
        row_length = len(line1data) # asumption that all rows are same length
        char_map_string = "0"
        for i in range(1,row_length):
            char_map_string += chr(int(line1data[i]))
        lexical_table = {}
        for i in range(1, len(rows)):
            line_data = rows[i].rstrip().split(',')
            if len(line_data) != row_length:
                continue # ignore malformed row
            current_state = int(line_data[0])
            column_lookup = {}
            for j in range(1, len(line_data)):
                if len(line_data[j]) == 0 or line_data[j][0] == 13:
                    state = -1
                else:
                    state = int(line_data[j])
                column_lookup[char_map_string[j]] = state
            lexical_table[current_state] = column_lookup
        return lexical_table

    def __parse_token_table(self):
        """
        Parses the token table from the file given in the constructor.
        @return: A token table map.
        """
        table_string = self.__read_file_as_string(self.token_table_filename)
        rows = table_string.split('\n')
        token_table = {}
        for i in range(0, len(rows)):
            line_tuple = rows[i].split(',')
            if len(line_tuple) != 2:
                continue # skip non tuple lines
            state = int(line_tuple[0])
            token = str(line_tuple[1])
            if len(token) == 0 or token[0] == 13:
                token = 'error'
            else:
                token = token.rstrip() # strip newline and carridge return
            token_table[state] = token
        return token_table
            

    def __parse_source_code(self):
        """
        Parses the source code from the file given in the constructor.
        @return: A string of the source code.
        """
        return self.__read_file_as_string(self.source_filename)

    def __parse_keywords(self):
        keywords_string = self.__read_file_as_string(self.keywords_table_filename)
        keywords_split = keywords_string.rstrip().split(',')
        keywords = [str(i) for i in keywords_split]
        return keywords

    def __validate(self):
        """
        Performs validation on the input by calling class helper functions. May
        terminate the program if a validation test fails.
        """
        self.__validate_csv(self.lexical_table_filename)
        self.__validate_csv(self.token_table_filename)
        self.__validate_csv(self.keywords_table_filename)
        self.__file_exists(self.lexical_table_filename)
        self.__file_exists(self.token_table_filename)
        self.__file_exists(self.source_filename)
        self.__file_exists(self.keywords_table_filename)

    def __validate_csv(self, filename):
        """
        Function ensures that the provided file has a .csv extension. Will raise
        value error if the provided file does not have a .csv extension.
        @param filename: The name of the file to check.
        """
        if len(filename) < 5 or filename[-4:] != ".csv":
            raise ValueError("expected a .csv file, got", filename, "instead")

    def __file_exists(self, filename):
        """
        Function ensures that the provided file exists. Will raise value error 
        if the provided file does not exist.
        @param filename: The name of the file to check.
        """
        if not os.access(filename, os.R_OK):
            raise ValueError("file",filename,"does not exist")

    def __read_file_as_string(self, filename):
        """
        Reads the provided file as a string.
        @param filename: Name of the file to read.
        @return: A string of the files contents.
        """
        with open(filename, 'r') as file_pointer:
            return file_pointer.read()