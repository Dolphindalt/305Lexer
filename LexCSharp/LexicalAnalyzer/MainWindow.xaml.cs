using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LexicalAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Lexer lexer;
        private List<Token> internalState;
        int eggCtr1;
        int eggCtr2;
        bool activeEgg;
        public MainWindow()
        {
            lexer = new Lexer(); // Init the lexer.
            internalState = new List<Token>(); // Set up an empty token stream as the internal state.
            eggCtr1 = 0;
            eggCtr2 = 0;
            activeEgg = false;
            InitializeComponent(); // Do microsoft magic behind my back without my permission.
        }

        /**
         * Open Scanning Table Button
         * Prompts the user for a scanning table csv file for input to the lexical analyzer.
         */
        private void btnOpenScanTable_Click(object sender, RoutedEventArgs e)
        {
            eggCtr1++;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                // Open the file to read line by line. 
                System.IO.StreamReader file =
                     new System.IO.StreamReader(openFileDialog.FileName);

                // Throw away the first line. 
                string output = "";
                string [] line1 = file.ReadLine().TrimEnd('\n').Split(',');
                string charIndexes = "";
                for (int i = 1; i < line1.Length; i++)
                {
                    char charDex = (char)Int32.Parse(line1[i]);
                    charIndexes += charDex;
                    output += ((int)charDex).ToString() + "\t";
                }
                output += "\n";

                SortedDictionary<int, SortedDictionary<char, int>> lexicalTable = new SortedDictionary<int, SortedDictionary<char, int>>();
                
                while (!file.EndOfStream)
                {
                    string [] row = file.ReadLine().TrimEnd('\n').Split(',');
                    int currentState = Int32.Parse(row[0]);
                    output += currentState.ToString() + "\t";
                    SortedDictionary<char, int> columnLookup = new SortedDictionary<char, int>();
                    for (int i = 1; i < row.Length; i++)
                    {
                        int state;
                        if (string.IsNullOrEmpty(row[i]) || row[i][0] == 13)
                        {
                            state = -1;
                        }
                        else
                        {
                            state = Int32.Parse(row[i]);
                        }
                        columnLookup.Add(charIndexes[i-1], state);
                        output += state.ToString() + "\t";
                    }
                    lexicalTable.Add(currentState, columnLookup);
                    output += "\n";
                }

                lexer.SetLexicalTable(lexicalTable);

                txtScanTable.Text = output; 
            }

        }

        /**
         * Open Token Table Button
         * Prompts the user for a token table csv file for input to the lexical analyzer.
         */
        private void btnOpenTokenTable_Click(object sender, RoutedEventArgs e)
        {
            if (eggCtr1 == 2 && eggCtr2 == 2)
            {
                egg();
            }
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                string output = "";
                SortedDictionary<int, string> tokenTable = new SortedDictionary<int, string>();
                string raw = File.ReadAllText(openFileDialog.FileName);
                string [] rows = raw.Split('\n');
                for (int i = 1; i < rows.Length; i++) // skip the first line
                {
                    string [] line = rows[i].Split(',');
                    if (line.Length < 2) // skip bad lines
                        continue;
                    if (line[0] == "\n" || line[1] == "")
                    { // newline at end of file or end of file, break
                        break;
                    }
                    int state = Int32.Parse(line[0]);
                    string token = line[1];
                    if (string.IsNullOrEmpty(token) || token[0] == 13)
                    {
                        token = "error";
                    }
                    token = token.TrimEnd('\n').TrimEnd('\r');
                    tokenTable.Add(state, token);
                    output += state + "\t" + token + "\n";
                }
                lexer.SetTokenTable(tokenTable);
                txtTokenTable.Text = output;
            }
        }

        /**
         * Open Source Code Button
         * Prompts the user for a source code file to be read as input for the lexical analyzer.
         */
        private void btnOpenSourceCode_Click(object sender, RoutedEventArgs e)
        {
            eggCtr2++;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                txtSourceCode.Text = File.ReadAllText(openFileDialog.FileName);
                lexer.SetSource(txtSourceCode.Text);
            }
        }

        /**
         * Activate Lexer Button
         * Pressing the activate lexer will perform the lexical analysis, but only if all inputs are properly provided.
         */
        private void btnLexerParse_Click(object sender, RoutedEventArgs e)
        {
            if (activeEgg)
            {
                activeEgg = false;
                furretvideo.Stop();
                furretvideo.Visibility = Visibility.Hidden;
                txtTokenStream.Text = "Press step to continue.";
                return;
            }
            internalState = lexer.AttemptLexicalAnalysis(); // Perform the lexical analysis.
            if (internalState.Count() == 0) // Check the internal state of the token stream for tokens.
            {
                bool[] Ready = lexer.getReadyArray();
                if (!Ready[0]) // Ready state zero corresponds to the source code being loaded.
                {
                    txtTokenStream.Text = "Please choose a source code.";
                }
                else if (!Ready[1]) // Ready state one corresponds to the scanning table being loaded.
                {
                    txtTokenStream.Text = "Please choose a scanning table.";
                }
                else if (!Ready[2]) // Ready state two corresponds to the token table being loaded.
                {
                    txtTokenStream.Text = "Please choose a token table.";
                }
                else // If all the states are loaded, but we have zero tokens, display an empty token stream.
                {    // This can occur if the user provides an empty file with no tokens.
                    txtTokenStream.Text = "The token stream is empty.";
                }
            }
            else // The internal state has tokens after performing analysis, so we can inform the user to use the step button.
            {
                txtTokenStream.Text = "Press step to continue.";
            }
        }

        /**
         * Step Button Function
         * Increments the current token into the users view. If not tokens exist, the user will be asked to provide input and activate the lexer.
         */
        private void btnLexerStep_click(object sender, RoutedEventArgs e)
        {
            if (internalState.Count() == 0) // The internal state count is zero, so we need to activate the lexer to get more tokens.
            {
                txtTokenStream.Text = "There are no tokens.\nRead in a lexical table, token table, and source code.\nOnce ready, press activate lexer to continue.";
            }
            else
            {
                Token current = internalState[0]; // Get the token in step t+1
                internalState.RemoveAt(0); // Remove the token at step t+1
                if (current.Type == "error") // If the token is an error, log relevent error information.
                {
                    txtTokenStream.Text = "Error at this step: " + current.Value;
                }
                else // If the token is not an error, we display the string form of the given token.
                {
                    txtTokenStream.Text = current.ToString();
                }
            }
        }

        private void egg()
        {
            Console.WriteLine("This is the easter egg");
            furretvideo.Visibility = Visibility.Visible;
            furretvideo.Play();
            txtTokenStream.Text = "Take a break and go for a walk. Don't let the tokens analyze you.\n\nHit activate lexer when you return. :3";
            activeEgg = true;
        }
    }
}
