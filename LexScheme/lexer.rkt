#lang racket

; Description: This program is a lexical analyzer for SchemeExpress. See the
; README.md for a more detailed program description.
; Author: Dalton Caron
; Date: November 9, 2019
; I dedicate this program to me mum.

(require csv-reading)
(require racket/file)

;-------------------------------------------------------------------------------
; BEGIN OF CSV READING SECTION
;-------------------------------------------------------------------------------

; Sets options for make-csv-reader-maker function.
(define make-csv-reader
    (make-csv-reader-maker
        '(
            (separator-chars #\,)
            (strip-leading-whitespace?  . #t)
            (strip-trailing-whitespace? . #t)
        )
    )
)

; Grabs the next row of the lexical table.
(define lexer-table-next-row
    (make-csv-reader (open-input-file "lexicalTable.csv"))
)

; Grabs the next row of the token table.
(define token-table-next-row
    (make-csv-reader (open-input-file "tokenTable.csv"))
)

; Grabs the next row of the keywords table.
(define keyword-table-next-row
    (make-csv-reader (open-input-file "keywords.csv"))
)

; The primary function for reading the lexical table. Returns a list containing
; each row in the format of (current_state (ascii next_state) (ascii next_state) ...)
(define read-lexical-table
    (lambda ()
        (let
            ((header (lexer-table-next-row)))
            (unless (null? header)
                (read-lexical-table-row header)
            )
        )
    )
)

; Reads all of the lexer table rows given the ascii character heading.
; header: ascii character header from the first row of the csv file.
(define read-lexical-table-row
    (lambda (header)
        (let
            ((row (lexer-table-next-row)))
            (if (null? row)
                '()
                (cons (parse-lexical-table-row header row) (read-lexical-table-row header))
            )
        )
    )
)

; Given the first row (a header of ascii characters), parses each row into a list,
; where the first element of the list is the current state, and the other elements
; are key value pairs of characters to next states.
; ((-1 current_state) (a 101) (b 102) (c error) etc...)
; header: The first row of the table containing ascii characters.
(define parse-lexical-table-row
    (lambda (header row)
        (if (null? header)
            '()
            (if (equal? (car header) -1)
                (cons (car row) (parse-lexical-table-row (cdr header) (cdr row)))
                (cons (list (car header) (replace-space-with (car row) 'error)) (parse-lexical-table-row (cdr header) (cdr row)))
            )
        )
    )
)

; Reads the token table csv file into a list of key value pairs. Each key value
; pair consists of a state and token type. States with no token type contain the
; error state.
(define read-token-table
    (lambda ()
        (let
            ((row (token-table-next-row)))
            (if (null? row)
                '()
                (cons (list (car row) (replace-space-with (car (cdr row)) 'error)) (read-token-table))
            )
        )
    )
)

(define read-keyword-list
    (lambda ()
        (keyword-table-next-row)
    )
)

; Replaces the given value with something else if the given value is an empty
; string.
; actual: the actual value to check against.
; replacement: what to replace the actual value with, given it is an empty 
; string.
(define replace-space-with
    (lambda (actual replacement)
        (if (equal? actual "")
            replacement
            actual
        )
    )
)

;-------------------------------------------------------------------------------
; BEGIN OF CSV TABLE DATA STRUCTURE FUNCTIONS
;-------------------------------------------------------------------------------

; Gets the first state from the lexical table.
; table: The lexical table.
(define startingState
    (lambda (table)
        (car (cdr (car(car table))))
    )
)

; Extracts the current state from the extracted row.
; row: An extracted row.
(define getState
    (lambda (row)
        (car (cdr (car row)))
    )
)

; Finds the row that states with currentState in the lexical table.
; currentState: The state corresponds to the row to search for.
; table: The lexical table.
(define extractRow
    (lambda (currentState table)
        (if (null? table)
            'row_error
            (if (equal? (getState (car table)) currentState)
                (car table)
                (extractRow currentState (cdr table))
            )
        )
    )
)

; Extracts state transition pairs from and extracted row.
; row: The row to extract transitions from.
(define getTransitions
    (lambda row
        (cdr row)
    )
)

; Returns the token type given a state and token table.
; state: The state to lookup a type for.
; tokenTable: A list of (state type) pairs.
(define getTokenType
    (lambda (state tokenTable)
        (let
            ((lookup (assoc state tokenTable)))
            (if (pair? lookup)
                (car (cdr lookup))
                'error
            )
        )
    )
)

; A utility function that acts as cdr for strings.
; str: The string to cdr.
(define string-cdr
    (lambda (str)
        (let
            ((len (string-length str)))
            (if (<= len 0)
                '()
                (substring str 1 len)
            )
        )
    )
)

;-------------------------------------------------------------------------------
; BEGIN OF LEXICAL ANALYZER FUNCTIONS
;-------------------------------------------------------------------------------

; The move function decides what action should be taken given the currentState
; and currentCharacter in the form of a tuple containing what action was
; chosen and other information related to the action.
; lexicalTable: The lexical table.
; tokenTable: The token table.
; currentState: The current state.
; currentCharacter: The current lexem.
(define move
    (lambda (lexicalTable tokenTable currentState currentCharacter)
        (let*
            (
                (extractedRow (extractRow currentState lexicalTable))
                (nextState 
                    (if (pair? extractedRow)
                        (if (equal? (assoc currentCharacter extractedRow) #f)
                            'error
                            (car (cdr (assoc currentCharacter extractedRow)))
                        )
                        'error
                    )
                )
                (token (getTokenType currentState tokenTable))
            )
            (if (or (equal? extractedRow 'error) (equal? nextState 'error))
                (if (null? token)
                    (list 'error_state)
                    (list 'recognize_state token)
                )
                (list 'move_state nextState)
            )
        )
    )
)

; Returns a (value, type) token pair from the source code input stream. Returns
; errors as a (err msg, error) token.
; currentState: The current state.
; source: The source code input stream.
; lexicalTable: The lexical table.
; tokenTable: The token table.
; image: A string containing the built token so far.
(define getToken
    (lambda (currentState source lexicalTable tokenTable image)
        (if (equal? (string-length source) 0)
            '()
            (let*
                (
                    (currentCharacter (~v (char->integer (string-ref source 0))))
                    (rawCurrentCharacter (string (string-ref source 0)))
                    (action (move lexicalTable tokenTable currentState currentCharacter))
                )
                (if (member 'move_state action)
                    ; the move action
                    (if (not (equal? (assoc currentState tokenTable) 'error))
                        (getToken (car (cdr action)) (string-cdr source) lexicalTable tokenTable (string-append image rawCurrentCharacter))
                        (getToken (car (cdr action)) (string-cdr source) lexicalTable tokenTable (string-append image rawCurrentCharacter))
                    )
                    (if (member 'recognize_state action)
                        ; the recognize action
                        (list image (car (cdr action)) source)
                        ; the error action
                        (list "encountered an error" action (string-cdr source))
                    )
                )
            )
        )
    )
)

; Concatenates all the tokens into a list. Eliminates whitespace and finds
; keyword tokens.
; source: The source code input stream.
; lexicalTable: The lexical table. 
; tokenTable: The token table.
; keywords: A list of keywords.
(define buildTokenStream
    (lambda (source lexicalTable tokenTable keywords)
        (if (equal? (string-length source) 0)
            '()
            (let
                ((token_return (getToken "0" source lexicalTable tokenTable "")))
                (if (null? token_return)
                    '()
                    (let*
                        (
                            
                            (token_value (car token_return))
                            (token_type (car (cdr token_return)))
                            (source (car (cdr (cdr token_return))))
                        )
                        (if (member token_type '("whitespace" "newline" "comment"))
                            ; ignore whitespace and comments
                            (buildTokenStream source lexicalTable tokenTable keywords)
                            ; else we include the token
                            (if (and (equal? token_type "identifier") (member token_value keywords))
                                ; recognize as a keyword
                                (cons (list token_value "keyword") (buildTokenStream source lexicalTable tokenTable keywords))
                                ; recognize as whatever
                                (cons (list token_value token_type) (buildTokenStream source lexicalTable tokenTable keywords))
                            )
                        )
                    )
                )
            )
        )
    )
)

; Executes the lexical analyzer.
(define lexicalAnalyzer
    (lambda ()
        (let
            (
                (lexicalTable (read-lexical-table))
                (tokenTable (read-token-table))
                (keywordList (read-keyword-list))
                (source (port->string (open-input-file (read-line))))
            )
            (display "Press enter to see the next token: ")
            (buildTokenStream source lexicalTable tokenTable keywordList)
        )
    )
)

; Displays each token in the token stream, prompting the user to hit enter to
; see a new token on each step.
; tokenstream: A list of tokens.
(define displayTokensNicely
    (lambda (tokenStream)
        (let
            ((token (car tokenStream)))
            (read-line)
            (display token)
            (newline)
            (unless (null? (cdr tokenStream))
                (displayTokensNicely (cdr tokenStream))
            )
        )
    )
)

; ------------------------------------------------------------------------------
; BEGIN OF MAIN PROGRAM ENTRY POINT
; ------------------------------------------------------------------------------

(display "Please enter a source code file name: ")
(newline)
(displayTokensNicely (lexicalAnalyzer))
(newline)
(display "End of token stream")