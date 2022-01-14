"""
The lexer file is responsible for the class that performs the lexical analysis.
Author: Dalton Caron
"""

class Lexer:
    """
    The class that will perform lexical analysis, given the proper inputs.
    """
    def __init__(self, lexical_table, token_table, source_code, keywords_list):
        """
        Sets the class variables for the tables and source code.
        """
        self.lexical_table = lexical_table
        self.token_table = token_table
        self.source_code = source_code
        self.keywords_list = keywords_list

    def perform_analysis(self):
        """
        The function that performs the lexical analysis.
        @return: A list of tokens representing the token stream.
        """
        self.current_token = 0
        tokens = []
        black_list = ["whitespace", "newline", "comment"]
        while self.current_token < len(self.source_code):
            token = self.__get_token()
            if token[1] in black_list:
                continue
            tokens.append(token)
        return tokens

    def __get_token(self):
        """
        Gets a single token from the source code.
        @return: A single token.
        """
        remembered_chars = ""
        current_state = 0
        image = ""
        remembered_state = 0
        while True:
            current_character = self.__get_character()
            action = self.__choose_action(current_state, current_character)
            if action == 0: # move
                if current_state in self.token_table.keys() and \
                    not self.token_table[current_state] == 'error':
                    # could be in a final state
                    remembered_state = current_state
                    remembered_chars = ""
                remembered_chars += current_character
                current_state = self.lexical_table[current_state][current_character]
            elif action == 1: # recognize
                token = self.token_table[current_state]
                if not self.current_token == len(self.source_code):
                    self.current_token -= 1 # unread last read token
                break
            else: # error
                if remembered_state != 0:
                    token = self.token_table[remembered_state]
                    self.current_token -= len(remembered_chars)
                    image = image[:-len(remembered_chars)]
                    break
                return ("error at lexem number " + str(self.current_token) + \
                    ": | current lexem " + str(current_character) + " | " \
                    "| current state " + str(current_state) + " | remembered state "\
                     + str(remembered_state) , "error")
            image += current_character
        token_tuple = (image, token)
        token_tuple = self.__keyword_check(token_tuple)
        return token_tuple

    def __choose_action(self, current_state, current_character):
        """
        Chooses an action. If the current state and current character map to a
        valid state, the action is a move. If not and the token table is a goal
        state, then we recognize. If none of these conditions are true, we flag
        an error state.
        @param current_state is an integer representing the current state.
        @param current_character is a character representing the current lexem.
        @return: 0 move, 1 recognize, or 2 error.
        """
        if current_state in self.lexical_table.keys() and \
            current_character in self.lexical_table[current_state].keys():
            move = self.lexical_table[current_state][current_character]
            if move != -1: # valid move, so we select the move action
                return 0
        # we did not get a valid move, so we try to recognize
        if current_state in self.token_table.keys() and \
            not self.token_table[current_state] == 'error':
            return 1
        # otherwise, we are in an error state
        return 2

    def __get_character(self):
        """
        Get a single character from the source code.
        @return: A character or 0 if the source code is exhausted.
        """
        if self.current_token < len(self.source_code):
            char = self.source_code[self.current_token]
            self.current_token += 1
            return char
        else:
            self.current_token += 1
            return 0
    
    def __keyword_check(self, token_tuple):
        """
        Checks a given token literal is a keyword, and changes the label as
        needed.
        @param token_tuple: A tuple of the literal token and token label.
        @return: The token tuple, modified if need be.
        """
        if token_tuple[1] == "identifier" and token_tuple[0] in self.keywords_list:
            return (token_tuple[0], "keyword")
        return token_tuple