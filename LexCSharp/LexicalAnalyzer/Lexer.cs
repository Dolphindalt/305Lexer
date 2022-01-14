using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/**
 * We gather in this class to escape the nightmare that is MainWindow.xaml.cs.
 * May the lord bless the data that did not make it out of the fray, and into this
 * holy vessel. Amen.
 * 
 * This class is the Lexical Analyzer. All lexical analysis takes place inside this class.
 */
namespace LexicalAnalyzer
{
    /**
     * A struct for storing the results of a single step of the lexical analysis.
     */
    struct Token
    {
        public string Value; // Token value. The actual token as a string.
        public string Type;  // What type of token we are beholding.
        public Token(string val, string typ)
        {
            Value = val; // The raw token value.
            Type = typ; // The type of the token, i.e. string, decimal, binary, etc.
        }
        // We use this ToString method for viewing the token.
        override public string ToString()
        {
            return "(" + Value + ", " + Type + ")";
        }
    }

    /**
     * Lexer handles everything that the lexical analyzer is suppose to,
     * given that it has the proper data to perform the analysis.
     */
    class Lexer
    {
        private string Source; // The source code as a string.
        private int CurrentToken; // The token index number that we are currently looking at.
        private SortedDictionary<int, SortedDictionary<char, int>> LexicalTable; // The lexer lookup table.
        private SortedDictionary<int, string> TokenTable; // The token type lookup table. State to token string.
        private bool[] Ready; // We use this to keep track of how many inputs we have ready.
        public Lexer()
        {
            Source = "";
            CurrentToken = 0;
            LexicalTable = null;
            TokenTable = null;
            Ready = new bool[3];
        }

        /**
         * This function checks that all input constraints are met. If so,
         * the function executes the lexical analyzer by calling PerformLexicalAnalysis.
         * If lexical analysis cannot be performed, an empty token stream is returned.
         */
        public List<Token> AttemptLexicalAnalysis()
        {
            if (Ready[0] && Ready[1] && Ready[2])
            {
                return this.PerformLexicalAnalysis();
            }
            return new List<Token>();
        }

        /**
         * PerformLexicalAnalysis does not actually perform the lexical analysis.
         * It wraps getToken so thaty it may store its output as a token stream,
         * disregarding tokens that are not important, such as white space.
         */
        private List<Token> PerformLexicalAnalysis()
        {
            this.CurrentToken = 0; // We begin analysis at the first character of the source code.
            List<Token> tokens = new List<Token>(); // Init an empty token stream.
            while (CurrentToken < Source.Length) // Analysis of the source code continues until all characters are used up.
            {
                Token token = this.GetToken();
                // Note that carrige return and newline fall into the token type newline.
                while (token.Type == "whitespace" || token.Type == "newline" || token.Type == "comment")
                { // Ensure we ignore comments, whitespace, and newlines.
                    if (CurrentToken < Source.Length)
                    {
                        token = GetToken();
                    }
                    else break;
                }
                if (!(token.Type == "whitespace") && !(token.Type == "newline") && !(token.Type == "comment"))
                    tokens.Add(token); // The if statement catches if the last token is a whitespace, comment, or newline.
            }
            return tokens; // Return the token stream.
        }

        /**
         * GetToken does the heavy lifting and actually performs the lexical analysis.
         * Returns a single Token read from the source code or an error token.
         */
        private Token GetToken()
        {
            string tok;
            char currentChar;
            string image;
            string rememberedChars = "";
            int currentState = 0;
            image = "";
            int rememberedState = 0;
            while (true)
            {
                currentChar = GetCharacter();
                int action = ChooseAction(currentChar, currentState);
                if (action == 0) // move
                {
                    if (TokenTable.ContainsKey(currentState) && TokenTable[currentState] != "error")
                    { // could be a final state
                        rememberedState = currentState;
                        rememberedChars = "";
                    }
                    rememberedChars += currentChar;
                    // ToLower in this line allows for upper case characters to be recognized. It is a hacking fix, but it works.
                    currentState = LexicalTable[currentState][currentChar.ToString().ToLower()[0]];
                }
                else if (action == 1) // recognize
                {
                    tok = TokenTable[currentState];
                    if (!(CurrentToken == Source.Length))
                    {
                        CurrentToken--; // unread current token
                    }
                    break; // escape inner loop
                }
                else // error
                {
                    if (rememberedState != 0)
                    {
                        tok = TokenTable[rememberedState];
                        for (int i = 0; i < rememberedChars.Length; i++)
                        {
                            CurrentToken--; // unread rememberedChars
                        }
                        // remove rememberedChars from image
                        image.Remove(image.Length - rememberedChars.Length);
                        break; // escape inner loop
                    }
                    // We errored hard. Return an error token describing the error.
                    return new Token("at lexem number " + CurrentToken.ToString() + ": current lexem " + currentChar.ToString(), "error");
                }
                image += currentChar;
            } // end inner loop
            return new Token(image, tok);
        }

        /**
         * There are three possible actions our lexical analyzer must make: move, recognize, or error.
         * Since we lack a table to handle these actions, this function exists to decide what state to take.
         */
        private int ChooseAction(char currentCharacter, int currentState)
        {
            try
            { // We start by checking the move action by checking if a valid move exists in our table given the state and character.
                int move = LexicalTable[currentState][currentCharacter.ToString().ToLower()[0]];
                if (move != -1) // If we have a valid move, we should move.
                {
                    return 0;
                }
                if (TokenTable.ContainsKey(currentState) && TokenTable[currentState] != "error")
                { // There is no valid move, so if we can recognize, we should.
                    return 1;
                }
                else
                { // We cannot move or recognize, so we try to error.
                    return 2;
                }
            }
            catch (Exception) // An invalid key was caught when trying to move.
            {                 // This implies move is not an action we can take.
                if (TokenTable.ContainsKey(currentState) && TokenTable[currentState] != "error")
                { // We try to recoginze then.
                    return 1;
                }
                else
                { // Failure to recognize results in an error state.
                    return 2;
                }
            }
        }
        
        /**
         * GetCharacter returns the next character in the source code and increments counters accordingly.
         * If there is no next character, the null character '\0' is returned instead.
         */
        private char GetCharacter()
        {
            if (CurrentToken < Source.Length)
            {
                return Source[CurrentToken++];
            }
            CurrentToken++;
            return '\0';
        }
        
        /**
         * SetSource takes a string and sets it to the source code for the lexical analyzer.
         * This function also adds a carrige return and newline to the end of the file if one does not exist.
         */
        public void SetSource(string source)
        {
            if (source.Length >= 2)
            {
                if (source.Substring(source.Length - 2, 2) != "\r\n")
                {
                    source += "\n\r";
                }
            }
            this.Source = source;
            this.Ready[0] = true; // Set the source code as ready.
        }

        /**
         * SetLexicalTable takes the lexical table as input and stores it for the lexical analyzer to use.
         */
        public void SetLexicalTable(SortedDictionary<int, SortedDictionary<char, int>> lexicalTable)
        {
            this.LexicalTable = lexicalTable;
            this.Ready[1] = true; // Set the lexical table as ready.
        }

        /**
         * SetTokenTable takes the token table as input and stores it for the lexical analyzer to use.
         */
        public void SetTokenTable(SortedDictionary<int, string> table)
        {
            this.TokenTable = table;
            this.Ready[2] = true; // Set the token table as ready.
        }

        // Getter for Ready state vector. Used when preparing analysis with the GUI.
        public bool[] getReadyArray()
        {
            return Ready;
        }
    }
}
