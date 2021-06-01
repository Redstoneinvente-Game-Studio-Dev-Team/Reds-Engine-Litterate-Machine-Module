using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.UI;

public class LitterateAI : MonoBehaviour
{
    [Header("Saving Parameters")]
    public string savingPath;

    public Text SavingPath;
    public Text Sufix;

    public GameObject loadBrainNotice;

    public Text messageNotice;
    public Text loadingPath;

    public string[] nouns;
    public string[] proNouns;
    public string[] verbs;
    public string[] adverbs;
    public string[] adjectives;
    public string[] prepositions;
    public string[] conjuctions;

    /// <summary>
    /// A Dictionary containing all wordTypes and their words
    /// </summary>
    public Dictionary<string, List<string>> wordTypesDictionary = new Dictionary<string, List<string>>();

    public string wordsHashTable;

    public Hashtable wordsHashTableHashVal = new Hashtable();

    [Header("AI Parameters")]
    public bool isTraining;
    public bool saveBrainFile;
    public bool useLoadedBrainFileInstead;
    public bool forceUseFactoryBrain;

    public int perLineRepetition;
    public int prevWordBias;
    public int nextWordBias;

    public InputField PerLineRepText;
    public InputField PrevWordBiasText;
    public InputField NextWordBiasText;

    public TextAsset BrainFile;

    public WordsScriptableObjects[] wordsScriptableObjects;

    [Header("Console Parameters")]
    public GameObject Console;
    public Text consoleDisp;

    [Header("Text Parameters")]
    public Text trainingTexts;
    public Text knownWordsText;

    public GameObject trainingTextObject;
    public GameObject reportText;

    public InputField trainingTextField;
    public InputField NounsText;
    public InputField PronounsText;
    public InputField VerbsText;
    public InputField AdverbsText;
    public InputField PrepositionsText;
    public InputField ConjunctionsText;
    public InputField AdjectivesText;
    public InputField NothingText;

    public string information;
    public string allWords;
    public string originalInformationInprocessed;

    public string[] lines;
    public string[] words;
    public string[] commonPrevWords;
    public string[] commonAfterWords;

    public List<string> uniqueWords = new List<string>();

    public int beforeWordsBias = 5;
    public int afterWordsBias = 10;

    public int[] positions;

    public WordTypesEnum[] wordTypesEnum;

    public bool found = false;
    public bool verified = false;

    string temp = "";

    public KnownWordsDetails knownWordsDetails = new KnownWordsDetails(new string[1]);

    public WordTypes wordTypes = new WordTypes();
    public WordTypes tempWordTypes = new WordTypes(new string[1], new string[1], new string[1], new string[1], new string[1], new string[1], new string[1], "", default, "");

    [Header("Question Parameters")]
    public float questionWeightBias = .2f;

    [HideInInspector]
    public bool finishedTraining;

    public string[] linesDebug;
    /*
     * Dictionary<"Word Being Refferenced", Dictionary<"Word Encountered", weight>>
     * weight = mean value this word has appeared before or after the refferenced word
    */
    public Dictionary<string, Dictionary<string, float>> wordsBeforeCount = new Dictionary<string, Dictionary<string, float>>();
    public Dictionary<string, Dictionary<string, float>> wordsAfterCount = new Dictionary<string, Dictionary<string, float>>();

    public Dictionary<string, Dictionary<string, int>> wordOccurencesPrev = new Dictionary<string, Dictionary<string, int>>();
    public Dictionary<string, Dictionary<string, int>> wordOccurencesAfter = new Dictionary<string, Dictionary<string, int>>();

    /* Format Word<KEY> : Occurence <VALUE> */
    Hashtable wordOccurence = new Hashtable();
    Hashtable wordHash = new Hashtable();

    public Question question = new Question();
    public Question loadedQuestion = new Question();

    public Dictionary<string, int> wordOccurenceDict = new Dictionary<string, int>();

    public bool LogQuestionsForTraining;
    public string pathToSave;
    public List<string> loggedList = new List<string>();

    /*
     * [\ -> partly done]
     * [\/ -> completed]
    * Hashtable format Word<KEY> : "meanOccurencePerLine, meanOccurenceForWholeText, mostCommonPrevWord1|weight > mostCommonPrevWord2|weight, mostCommonNextWord1|weight > mostCommonNextWord2|weight"<VALUE>
    * Replace Words With Type and create a line based formula,\/
    * store in a hashtable and store in a file as JSON,\/
    * load at startup for reference,\/
    * store occurences of each word types\/
    * try to learn about how human understands and learn languages,
    * combine it with ml and neural network
    * using weights as frequency for each word types, per word basis,\/
    * storing weights for each word and load at startup\/
    * GPT3
    */

    //Make learning system

    public struct WordTypes 
    {
        public string[] nouns;
        public string[] proNouns;
        public string[] verbs;
        public string[] adverbs;
        public string[] adjectives;
        public string[] prepositions;
        public string[] conjuctions;
        public string words;
        public KnownWordsDetails knownWordsDetails;
        public string[] fullWordsDetails;
        public string questionSyntax;

        public WordTypes(string[] nouns, string[] proNouns, string[] verbs, string[] adverbs, string[] adjectives, string[] prepositions, string[] conjuctions, string words, KnownWordsDetails knownWordsDetails, string questionSyntax, string[] fullWordsDetails = null)
        {
            this.nouns = nouns;
            this.proNouns = proNouns;
            this.verbs = verbs;
            this.adverbs = adverbs;
            this.adjectives = adjectives;
            this.prepositions = prepositions;
            this.conjuctions = conjuctions;
            this.words = words;
            this.knownWordsDetails = knownWordsDetails;
            this.fullWordsDetails = knownWordsDetails.fullWordsDetails;
            this.questionSyntax = questionSyntax;
        }
    }

    public struct KnownWordsDetails 
    {
        public string[] fullWordsDetails;

        public KnownWordsDetails(string[] fullWordsDetails)
        {
            this.fullWordsDetails = fullWordsDetails;
        }
    }

    /// <summary>
    /// This class contains everything concerning creating and learning questions
    /// </summary>
    public class Question 
    {
        /*
          question object syntax in brain file
          |---------Question 1---------------|----------Question 2---------------|
          WordsAfter1,WordsAfter2,WordsAfter3;WordsAfter1,WordsAfter2,WordsAfter3;
          |----------^-----------------------^-----------------------------------|
          |--[After Words Seperator]-[Question Seperator]------------------------|
        */

        public List<string> questionSyntaxJSON = new List<string>();

        /// <summary>
        /// This contains the full question syntax with question descriptors, weights, pos and word types(including words) and weight threshold
        /// </summary>
        public List<string> fullQuestionSyntax = new List<string>();

        /// <summary>
        /// On Start, initialise this dictionary with all Question Descriptors and word afters
        /// </summary>
        public Dictionary<string, string> questionSyntaxData = new Dictionary<string, string>();

        /// <summary>
        /// A list containing the question syntaxes in the form QuestionDescriptor word(WordType)(weight)(position)
        /// </summary>
        public List<string> questionSyntaxLoaded = new List<string>();

        /// <summary>
        /// It is the weight of the question syntax, if the AI builds a question based on this syntax, this would be the minimum weight needed (Validation Only) 
        /// Used in : <seealso cref="SetQuestionSyntax(string, string, Dictionary{string, int})"/>
        /// </summary>
        public float weightThreshold = 0;

        /// <summary>
        /// A list of Question Syntax containing loaded syntaxes for this object
        /// </summary>
        public List<QuestionSyntax> loadedQuestionSyntaxes = new List<QuestionSyntax>();

        public Questions questionDescriptor;

        public WordTypesEnum[] wordTypes;

        QuestionSyntax qS = new QuestionSyntax();
        public QuestionObject qsObj = new QuestionObject(new string[0], "");

        public float questionWeight;

        /// <summary>
        /// Stores the JSON for all learned questions, to be stored inside of brain file
        /// </summary>
        public string questionObjectJSON;

        /// <summary>
        /// This method is for setting question syntax 
        /// <para><paramref name="data"/>: The raw data entered by the user.</para>
        /// <para><paramref name="wordTypesData"/>: The data after being analysed, should contain information about the words, syntax : pronoun noun verb, without question descriptor.</para>
        /// <para><paramref name="wordOccurences"/>: A Dictionary of integers containing the words and their amount of occurences in the text.</para>
        /// </summary>
        /// <param name="data">The raw data entered by the user</param>
        /// <param name="wordTypesData">The data after being analysed, should contain information about the words, syntax : pronoun noun verb, without question descriptor</param>
        /// <param name="wordOccurences">A Dictionary of integers containing the words and their amount of occurences in the text</param>
        public void SetQuestionSyntax(string data, string wordTypesData, Dictionary<string, int> wordOccurences)
        {
            /*
             * Get word types
             * Store in array
             * Form question syntax : Question_Descriptor Word(Word_Type)(weight)(position)  Word(Word_Type)(weight)(position)
             * Store as JSON
            */

            string questionDescriptorString = data.Split(" "[0])[0];
            string[] wordTypesTemp = wordTypesData.Split(" "[0]);

            List<WordTypesEnum> tempWordType = new List<WordTypesEnum>();

            List<string> tempWordTypePos = new List<string>();

            List<int> weights = new List<int>();
            string tempData = "";

            //Adding word types to array
            foreach (string item in wordTypesTemp)
            {
                if (item != "" && item != " ")
                {
                    Debug.Log("item = " + item);
                    tempWordType.Add((WordTypesEnum)Enum.Parse(typeof(WordTypesEnum), item, true));
                }
            }

            wordTypes = tempWordType.ToArray();

            //Storing Question Descriptor
            string[] quests = Enum.GetNames(typeof(Questions));
            int QuestionDescriptorPos = 0;

            int count = 0;
            foreach (string questionEnumName in quests)
            {
                count = 0;
                foreach (string item in data.Split(" "[0]))
                {
                    if (questionEnumName.ToLower() == item.ToLower())
                    {
                        QuestionDescriptorPos = count;
                    }
                    count++;
                }
            }   //Storing position of question descriptor

            count = 0;
            int i = 0;
            int weight = 0;
            data = data.Replace("?", "");
            tempData = data.Replace(data.Split(" "[0])[QuestionDescriptorPos] + " ", "");     //<---- Replacing question descriptor
            Debug.Log("tempData = " + tempData);
            string wordAfter = tempData.Split(" "[0])[0];
            Debug.Log("Word after Question Descriptor = " + wordAfter);

            foreach (string word in tempData.Split(" "[0]))     //<--- Looping through all words contained in data
            {
                Debug.Log("(Set Question Syntax) : word = " + word + ", i = " + i + ",  wordTypesData's length = " + wordTypesData.Length);
                if (/*QuestionDescriptorPos != count && */word != "" && word != " ")
                {
                    //If not question descriptor
                    if (!wordOccurences.ContainsKey(word.Replace(" ", "")))
                    {
                        wordOccurences[word.Replace(" ", "")] = 1;
                    }

                    weight = (count + wordOccurences[word.Replace(" ", "")]) / 2;    //Calculating weight based on position and occurence
                    weights.Add(weight);    //<---- Storing the weight of this word, used also in calculating weight threshold

                    tempWordTypePos.Add(word + "(" + wordTypesData.Split(" "[0])[i + 1] + ")" + "(" + weight + ")" + "(" + count + ")");     //<--- Making question syntax word(wordType)(weight)(posInSentence)
                    i++;
                }
                
                count++;
            }   //Storing positions of each words and combining them with their types

            //Storing word after, getting first word apart from question descriptor
            //Getting and storing "word after" for the question descriptor
            try
            {
                questionSyntaxData[questionDescriptorString] += wordAfter + ",";     //<--- Contains all question descriptors and their corresponding word details
            }
            catch (Exception ex)
            {
                Debug.LogFormat("[LitterateAI]: questionSyntaxData was not initialised on start, probably the brain file is outdated!", questionSyntaxData);
                Debug.Log("Error : " + ex.Message);
                questionSyntaxData[questionDescriptorString] = wordAfter + ",";     //<--- Contains all question descriptors and their corresponding word details
            }

            //Calculating Weight Threshold
            foreach (int weightNumber in weights)
            {
                weightThreshold += weightNumber;
            }

            weightThreshold = weightThreshold / weights.Count;

            //Store everything in a JSON
            qS = new QuestionSyntax(questionDescriptor, tempWordType.ToArray(), weightThreshold, tempWordTypePos.ToArray());
            questionSyntaxJSON.Add(JsonUtility.ToJson(qS));
        }

        public struct QuestionSyntax
        {
            public Questions question;
            public WordTypesEnum[] wordTypes;
            public float questionWeightThreshold;
            public string[] questionSyntax;

            public QuestionSyntax(Questions question, WordTypesEnum[] wordTypes, float weightThreshold, string[] questionSyntax)
            {
                this.wordTypes = wordTypes;
                this.question = question;
                this.questionWeightThreshold = weightThreshold;
                this.questionSyntax = questionSyntax;
            }
        }

        /// <summary>
        /// QuestionObject stores all jsons of learned questions. Use this when training has been completed, and a list of all question JSONS has been prepared. QuestionData stores reference of weight and words after for each syntax
        /// </summary>
        public struct QuestionObject
        {
            public string[] questionJSONS;
            public string questionData;

            public QuestionObject(string[] questionJSONS, string questionData)
            {
                this.questionJSONS = questionJSONS;
                this.questionData = questionData;
            }
        }

        /// <summary>
        /// Use this method to quickly generate a questionObject JSON for storing
        /// </summary>
        public void FinishQuestionTraining()
        {
            Debug.Log("Storing Question Syntaxes...");
            string questionData = "";

            foreach (string key in questionSyntaxData.Keys)
            {
                if (questionSyntaxData[key] != "" & questionSyntaxData[key] != " ")
                {
                    questionData += questionSyntaxData[key] + ";";
                }
            }

            QuestionObject qsObject = new QuestionObject(questionSyntaxJSON.ToArray(), questionData);
            qsObj = qsObject;
            questionObjectJSON = JsonUtility.ToJson(qsObject);
        }

        /// <summary>
        /// This method is called to prepare question syntaxes to be stored inside of the brain file.
        /// <para>Loading question syntax from brain file:</para>
        /// <seealso cref="LoadQuestionSyntax"/>
        /// </summary>
        public void StoreQuestionSyntax()
        {

        }

        /// <summary>
        /// This method is called to load question syntaxes stored inside of the brain file.
        /// <para>Stores question syntax in the form Question Descriptor word(wordType)(weight)(posInSentence) in <see cref="questionSyntaxLoaded"/></para>
        /// <para>Storing question syntax to brain file:</para>
        /// <seealso cref="FinishQuestionTraining"/>
        /// </summary>
        /// <param name="json">The JSON stored in the brain file</param>
        public void LoadQuestionSyntax(string json)
        {
            if (json != "" && json != " ")
            {
                Debug.Log("JSON =  " + json);

                try
                {
                    qsObj = JsonUtility.FromJson<QuestionObject>(json);
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.Message);
                }

                string[] tempQuestionData = qsObj.questionData.Split(";"[0]);

                string[] QuestionDescs = Enum.GetNames(typeof(Questions));

                string[] questionsJSONS = qsObj.questionJSONS;

                foreach (string jsonTemp in questionsJSONS)
                {
                    QuestionSyntax questionSyntaxTemp = new QuestionSyntax();

                    try
                    {
                        questionSyntaxTemp = JsonUtility.FromJson<QuestionSyntax>(jsonTemp);
                    }
                    catch (Exception)
                    {
                        try
                        {
                            questionSyntaxTemp.question = JsonUtility.FromJson<QuestionSyntax>(jsonTemp).question;
                        }
                        catch (Exception)
                        {
                            questionSyntaxTemp.question = default;
                        }

                        try
                        {
                            questionSyntaxTemp.questionWeightThreshold = JsonUtility.FromJson<QuestionSyntax>(jsonTemp).questionWeightThreshold;
                        }
                        catch (Exception)
                        {
                            questionSyntaxTemp.questionWeightThreshold = default;
                        }

                        try
                        {
                            questionSyntaxTemp.wordTypes = JsonUtility.FromJson<QuestionSyntax>(jsonTemp).wordTypes;
                        }
                        catch (Exception)
                        {
                            questionSyntaxTemp.wordTypes = default;
                        }

                        try
                        {
                            questionSyntaxTemp.questionSyntax = JsonUtility.FromJson<QuestionSyntax>(jsonTemp).questionSyntax;
                        }
                        catch (Exception)
                        {
                            questionSyntaxTemp.questionSyntax = new string[0];
                        }
                    }

                    try
                    {
                        foreach (string syntax in questionSyntaxTemp.questionSyntax)
                        {
                            questionSyntaxLoaded.Add(syntax);
                        }
                    }
                    catch (Exception)
                    {
                        Debug.Log("No elements");
                    }

                    loadedQuestionSyntaxes.Add(questionSyntaxTemp);
                }

                foreach (string nameTemp in QuestionDescs)
                {
                    Debug.Log("Questions setting question syntax data");
                    if (!questionSyntaxData.ContainsKey(nameTemp))
                    {
                        questionSyntaxData.Add(nameTemp, "");
                    }
                }

                int i = 0;
                foreach (string item in tempQuestionData)
                {
                    if (item != "" && item != " ")
                    {
                        //Item = question syntax WordsAfter
                        questionSyntaxData[QuestionDescs[i]] = item;
                    }
                    i++;
                }
            }
        }

        /// <summary>
        /// Use this method to mix syntax data from brain to that of the newly generated one.New data will be added to the <paramref name="oldObject"/>
        /// </summary>
        /// <param name="oldObject">The Question Object from the brain</param>
        /// <param name="newObjec">The Question Object to be added</param>
        public void MixOldAndNewQuestionSyntax(QuestionObject oldObject, QuestionObject newObject)
        {
            string qsDataOld = oldObject.questionData;
            qsDataOld += newObject.questionData;

            qsObj.questionData = qsDataOld;

            List<string> tempHSON = new List<string>();
            foreach (string item in qsObj.questionJSONS)
            {
                tempHSON.Add(item);
            }

            foreach (string item in newObject.questionJSONS)
            {
                tempHSON.Add(item);
            }

            qsObj.questionJSONS = new string[0];
            qsObj.questionJSONS = tempHSON.ToArray();

            questionObjectJSON += JsonUtility.ToJson(qsObj);
        }

        /// <summary>
        /// Use this method to get the <see cref="questionWeight"/> of the <paramref name="questionData"/>
        /// <para><paramref name="questionData"/> : The question to be used</para>
        /// <para><paramref name="wordOccurences"/> : A dictionary containing the weights of words</para>
        /// </summary>
        /// <param name="questionData">The question to be used</param>
        /// <param name="wordOccurences">A dictionary containing the weights of words</param>
        public void VerifyQuestionIntegrity(string questionData, Dictionary<string, int> wordOccurences)
        {
            int count = 0;
            float weight = 0;
            foreach (string item in questionData.Split(" "[0]))
            {
                if (item != "" && item != " ")
                {
                    weight += (count + (wordOccurences.ContainsKey(item.Replace(" ", "")) ? wordOccurences[item.Replace(" ", "")] : 0)) / 2;    //Calculating weight based on position and occurence
                    count++;
                }
            }

            questionWeight = weight / (count - 1);
            Debug.Log("Question Weight = " + questionWeight);
        }
    }

    /// <summary>
    /// To be used to update or set the weight of a word in the dictionary
    /// </summary>
    /// <param name="wordReferenced">The word that is being referenced, the KEY of the dictionary</param>
    /// <param name="wordEncountered">The word to be added or updated</param>
    /// <param name="occurence">The amount to add as occurence (weight)</param>
    /// <param name="isAfter">Is the word encountered after the word being referenced?</param>
    /// <returns>Returns an updated value for this dictionary, per word basis</returns>
    public /*Dictionary<string, Dictionary<string, float>>*/ void GetSetWeight(string wordReferenced, string wordEncountered, float occurence, bool isAfter)
    {
        Dictionary<string, float> WeightDictionary = new Dictionary<string, float>();   //weight part
        Dictionary<string, Dictionary<string, float>> weightDictionary = new Dictionary<string, Dictionary<string, float>>();   //word part

        float weight = 0;

        if (isAfter)
        {
            if (wordsAfterCount.ContainsKey(wordReferenced))
            {
                if (wordsAfterCount[wordReferenced].ContainsKey(wordEncountered))
                {
                    Debug.Log("Dictionary contains the word");
                    float occurenceOriginal = wordsAfterCount[wordReferenced][wordEncountered];
                    weight = (occurence + occurenceOriginal) / 2;
                }
                else
                {
                    Debug.Log("Dictionary does not contain the word");
                    weight = occurence + 1;
                }

                WeightDictionary.Add(wordEncountered, weight);
                wordsAfterCount[wordReferenced] = WeightDictionary;
            }
            else
            {
                weight = occurence + 1;

                WeightDictionary.Add(wordEncountered, weight);
                wordsAfterCount.Add(wordReferenced, WeightDictionary);
            }
        }
        else
        {
            if (wordsBeforeCount.ContainsKey(wordReferenced))
            {
                if (wordsBeforeCount[wordReferenced].ContainsKey(wordEncountered))
                {
                    Debug.Log("Dictionary contains the word");
                    float occurenceOriginal = wordsBeforeCount[wordReferenced][wordEncountered];
                    weight = (occurence + occurenceOriginal) / 2;
                }
                else
                {
                    Debug.Log("Dictionary does not contain the word");
                    weight = occurence + 1;
                }

                WeightDictionary.Add(wordEncountered, weight);
                wordsBeforeCount[wordReferenced] = WeightDictionary;
            }
            else
            {
                weight = occurence + 1;

                WeightDictionary.Add(wordEncountered, weight);
                wordsBeforeCount.Add(wordReferenced, WeightDictionary);
            }
        }

        //return weightDictionary;
    }

    private void Start()
    {
        if (forceUseFactoryBrain)
        {
            PlayerPrefs.SetInt("useLoadedBrain", 1);
        }

        if (PlayerPrefs.GetInt("perLineRepetition") == 0)
        {
            PlayerPrefs.SetInt("perLineRepetition", 10);
        }

        if (PlayerPrefs.GetInt("useLoadedBrain") == 0)
        {
            //Use custom brain
            if (File.Exists(PlayerPrefs.GetString("brainPath")))
            {
                savingPath = PlayerPrefs.GetString("brainPath");
                //Read from existing brain file
                wordTypes = JsonUtility.FromJson<WordTypes>(File.ReadAllText(savingPath));

                UnloadJSONToLocal();
            }
            else
            {
                PlayerPrefs.SetInt("useLoadedBrain", 1);

                if (File.Exists(savingPath))
                {
                    //Read from existing brain file
                    wordTypes = JsonUtility.FromJson<WordTypes>(BrainFile.text);

                    UnloadJSONToLocal();
                }
            }
        }
        else
        {
            if (!useLoadedBrainFileInstead)
            {
                if (File.Exists(savingPath))
                {
                    //Read from existing brain file
                    wordTypes = JsonUtility.FromJson<WordTypes>(File.ReadAllText(savingPath));

                    UnloadJSONToLocal();
                }
            }
            else
            {
                //Read from existing brain file
                wordTypes = JsonUtility.FromJson<WordTypes>(BrainFile.text);

                UnloadJSONToLocal();
            }
        }

        updateKnownWords();
        LoadKnownWordDetails();
    }

    public void Update()
    {
        if (PlayerPrefs.GetInt("perLineRepetition") != perLineRepetition)
        {
            perLineRepetition = PlayerPrefs.GetInt("perLineRepetition");
            PerLineRepText.text = "" + perLineRepetition;
        }

        if (PlayerPrefs.GetInt("prevWordsBias") != prevWordBias)
        {
            prevWordBias = PlayerPrefs.GetInt("prevWordsBias");
            PrevWordBiasText.text = "" + prevWordBias;
        }

        if (PlayerPrefs.GetInt("nextWordBias") != nextWordBias)
        {
            nextWordBias = PlayerPrefs.GetInt("nextWordBias");
            NextWordBiasText.text = "" + nextWordBias;
        }

        if (isTraining)
        {
            isTraining = false;
            Train();
        }

        if (saveBrainFile)
        {
            saveBrainFile = false;
            SaveBrainState();
        }
    }

    /// <summary>
    /// This method is used to update the kown words from the brain file to the UI in the settings page
    /// </summary>
    public void updateKnownWords()
    {
        knownWordsText.text = "";

        knownWordsText.text = "Nouns : ";
        foreach (string item in nouns)
        {
            knownWordsText.text = knownWordsText.text + item + " , ";
        }

        knownWordsText.text = knownWordsText.text + "\n" + "Pronouns : ";
        foreach (string item in proNouns)
        {
            knownWordsText.text = knownWordsText.text + item + " , ";
        }

        knownWordsText.text = knownWordsText.text + "\n" + "Verbs : ";
        foreach (string item in verbs)
        {
            knownWordsText.text = knownWordsText.text + item + " , ";
        }

        knownWordsText.text = knownWordsText.text + "\n" + "Adverbs : ";
        foreach (string item in adverbs)
        {
            knownWordsText.text = knownWordsText.text + item + " , ";
        }

        knownWordsText.text = knownWordsText.text + "\n" + "Prepositions : ";
        foreach (string item in prepositions)
        {
            knownWordsText.text = knownWordsText.text + item + " , ";
        }

        knownWordsText.text = knownWordsText.text + "\n" + "Conjunctions : ";
        foreach (string item in conjuctions)
        {
            knownWordsText.text = knownWordsText.text + item + " , ";
        }
    }

    /// <summary>
    /// This method is used to train the AI to learn new words.
    /// </summary>
    public void Train()
    {
        finishedTraining = false;
        //Start Training, show report
        trainingTextObject.SetActive(true);
        reportText.gameObject.SetActive(false);

        //Get Information
        information = trainingTexts.text;

        //Split by lines
        lines = information.Split("."[0]);

        string[] tempLines = lines;
        lines = new string[lines.Length - 1];

        for (int z = 0; z < lines.Length; z++)
        {
            lines[z] = tempLines[z];
        }

        int lineNum = 0;
        foreach (string line in lines)
        {
            for (int q = 0; q < perLineRepetition; q++)
            {
                //Repeating n times, to get better results
                int k = 0;
                if (line != "" || line != " ")
                {
                    if (q == 0)
                    {
                        for (int i = 0; i < words.Length; i++)
                        {
                            //Assigning positions
                            positions[i] = i;
                            wordTypesEnum[i] = WordTypesEnum.nothing;
                        }
                    }

                    //Split each words
                    words = line.Split(" "[0]);

                    positions = new int[words.Length];
                    wordTypesEnum = new WordTypesEnum[words.Length];

                    //Initialising each word type as nothing before overwriting, only if first run
                    if (q == 0)
                    {
                        for (int s = 0; s < wordTypesEnum.Length; s++)
                        {
                            wordTypesEnum[s] = WordTypesEnum.nothing;
                        }
                    }

                    //Identify already known words
                    foreach (string word in words)
                    {
                        Debug.Log("Word = " + word + ", position " + k);
                        //Finding already known words
                        found = false;

                        foreach (string wordItem in nouns)
                        {
                            if (word.ToLower() == wordItem.ToLower())
                            {
                                found = true;
                                Debug.Log(word + " is a noun!");
                                wordTypesEnum[k] = WordTypesEnum.noun;
                            }
                        }

                        if (!found)
                        {
                            foreach (string wordItem in proNouns)
                            {
                                if (word.ToLower() == wordItem.ToLower())
                                {
                                    found = true;
                                    Debug.Log(word + " is a pronoun!");
                                    wordTypesEnum[k] = WordTypesEnum.pronoun;
                                }
                            }
                        }

                        if (!found)
                        {
                            foreach (string wordItem in verbs)
                            {
                                if (word.ToLower() == wordItem.ToLower())
                                {
                                    found = true;
                                    Debug.Log(word + " is a verb!");
                                    wordTypesEnum[k] = WordTypesEnum.verb;
                                }
                            }
                        }

                        if (!found)
                        {
                            foreach (string wordItem in adverbs)
                            {
                                if (word.ToLower() == wordItem.ToLower())
                                {
                                    found = true;
                                    Debug.Log(word + " is an adverbs!");
                                    wordTypesEnum[k] = WordTypesEnum.adverb;
                                }
                            }
                        }

                        if (!found)
                        {
                            foreach (string wordItem in adjectives)
                            {
                                if (word.ToLower() == wordItem.ToLower())
                                {
                                    found = true;
                                    Debug.Log(word + " is an adjective!");
                                    wordTypesEnum[k] = WordTypesEnum.adjective;
                                }
                            }
                        }

                        if (!found)
                        {
                            foreach (string wordItem in prepositions)
                            {
                                if (word.ToLower() == wordItem.ToLower())
                                {
                                    found = true;
                                    Debug.Log(word + " is a preposition!");
                                    wordTypesEnum[k] = WordTypesEnum.preposition;
                                }
                            }
                        }

                        if (!found)
                        {
                            foreach (string wordItem in conjuctions)
                            {
                                if (word.ToLower() == wordItem.ToLower())
                                {
                                    found = true;
                                    Debug.Log(word + " is a conjuction!");
                                    wordTypesEnum[k] = WordTypesEnum.conjuction;
                                }
                            }
                        }

                        k++;
                    }

                    //Trying to get unknown word's type
                    int j = 0;
                    foreach (WordTypesEnum typeOfWord in wordTypesEnum)
                    {
                        //Only change if nothing
                        if (typeOfWord == WordTypesEnum.nothing)
                        {
                            int nextWordIndex = j + 1;
                            int thisWordIndex = j;
                            int prevWordIndex = j - 1;

                            if (thisWordIndex > 0 && thisWordIndex < words.Length - 1)
                            {
                                //Not in first position
                                //Find 1 after
                                if (wordTypesEnum[nextWordIndex] != WordTypesEnum.nothing)
                                {
                                    //Check if next word is verified
                                    verifyWord(words[nextWordIndex]);
                                    bool stored = false;

                                    if (verified)
                                    {
                                        //If verified, store this word's type accordingly
                                        foreach (WordsScriptableObjects sc in wordsScriptableObjects)
                                        {
                                            //Find matching scriptable objects
                                            if (!stored)
                                            {
                                                if (sc.wordTypesEnum == wordTypesEnum[nextWordIndex])
                                                {
                                                    //If matches with next word's type, get the value before. [0 is before and 1 is after]
                                                    stored = true;
                                                    wordTypesEnum[thisWordIndex] = sc.beforeAfterFormulaType[0];
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //If not verified, check for any verified words after this one
                                        bool foundVerified = false;
                                        for (int h = nextWordIndex; h < words.Length; h++)
                                        {
                                            int targetVerifiedWordIndex = 0;
                                            //Check for any verified word
                                            verifyWord(words[nextWordIndex]);

                                            if (verified)
                                            {
                                                targetVerifiedWordIndex = h;
                                                foundVerified = true;

                                                //Store each word's type accordingly, from verified one to this word
                                                for (int e = targetVerifiedWordIndex - 1; e >= thisWordIndex; e--)
                                                {
                                                    stored = false;
                                                    foreach (WordsScriptableObjects sc in wordsScriptableObjects)
                                                    {
                                                        //Find matching scriptable objects
                                                        if (!stored)
                                                        {
                                                            if (sc.wordTypesEnum == wordTypesEnum[e + 1])   //e + 1, as value of word after this one
                                                            {
                                                                //If matches with target word's type, get the value before. [0 is before and 1 is after]
                                                                if (e + 1 != targetVerifiedWordIndex)
                                                                {
                                                                    //If below, then compate to make sure of word type
                                                                    if (sc.beforeAfterFormulaType[1] == wordTypesEnum[e + 1])
                                                                    {
                                                                        stored = true;
                                                                        wordTypesEnum[e] = sc.beforeAfterFormulaType[0];
                                                                    }
                                                                    else
                                                                    {
                                                                        //Does not match, do not store
                                                                        stored = false;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    stored = true;
                                                                    wordTypesEnum[e] = sc.beforeAfterFormulaType[0];
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        //If did not find any verified words, simple store this word's type accordingly
                                        if (!foundVerified)
                                        {
                                            stored = false;
                                            foreach (WordsScriptableObjects sc in wordsScriptableObjects)
                                            {
                                                //Find matching scriptable objects
                                                if (!stored)
                                                {
                                                    if (sc.wordTypesEnum == wordTypesEnum[nextWordIndex])
                                                    {
                                                        //If matches with next word's type, get the value before. [0 is before and 1 is after]
                                                        stored = true;
                                                        wordTypesEnum[thisWordIndex] = sc.beforeAfterFormulaType[0];
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //If nothing, wait until next repetition...
                                }

                                //Find 1 before
                                if (wordTypesEnum[prevWordIndex] != WordTypesEnum.nothing)
                                {
                                    //Check if last word is verified
                                    verifyWord(words[prevWordIndex]);
                                    bool stored = false;

                                    if (verified)
                                    {
                                        //If verified, store this word's type accordingly
                                        foreach (WordsScriptableObjects sc in wordsScriptableObjects)
                                        {
                                            if (wordTypesEnum[thisWordIndex] != WordTypesEnum.nothing)
                                            {
                                                //See if word type is matching...
                                                if (sc.wordTypesEnum == wordTypesEnum[thisWordIndex])
                                                {
                                                    if (sc.beforeAfterFormulaType[0] != wordTypesEnum[prevWordIndex])
                                                    {
                                                        foreach (WordsScriptableObjects item in wordsScriptableObjects)
                                                        {
                                                            if (item.wordTypesEnum == wordTypesEnum[prevWordIndex])
                                                            {
                                                                //Find matching scriptable objects
                                                                if (!stored)
                                                                {
                                                                    //Check if 2 words from here is verified, if next word is not last.
                                                                    if (words.Length < nextWordIndex + 1)
                                                                    {
                                                                        verifyWord(words[nextWordIndex + 1]);
                                                                    }

                                                                    if (!verified)
                                                                    {
                                                                        if (sc.wordTypesEnum == wordTypesEnum[prevWordIndex])
                                                                        {
                                                                            //If matches with last word's type, get the value after. [0 is before and 1 is after]
                                                                            stored = true;
                                                                            wordTypesEnum[thisWordIndex] = item.beforeAfterFormulaType[1];
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        //If both is verified, cannot decide, output to trainer
                                                                        if (sc.wordTypesEnum == wordTypesEnum[prevWordIndex])
                                                                        {
                                                                            //If matches with last word's type, get the value after. [0 is before and 1 is after]
                                                                            stored = false ;
                                                                            consoleDisp.text = consoleDisp.text + " Cannot find relation between " + sc.wordTypesEnum.GetType().GetEnumName(sc.wordTypesEnum) + " and " + wordTypesEnum[prevWordIndex].GetType().GetEnumName(wordTypesEnum[prevWordIndex]) + " >>>" +
                                                                                "";
                                                                            //wordTypesEnum[thisWordIndex] = sc.beforeAfterFormulaType[1];
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                //Find matching scriptable objects
                                                if (!stored)
                                                {
                                                    if (sc.wordTypesEnum == wordTypesEnum[prevWordIndex])
                                                    {
                                                        //If matches with last word's type, get the value after. [0 is before and 1 is after]
                                                        stored = true;
                                                        wordTypesEnum[thisWordIndex] = sc.beforeAfterFormulaType[1];
                                                    }
                                                }
                                            }
                                        }

                                        if (!stored)
                                        {
                                            //Cannot decide, no relationship found
                                        }
                                    }
                                    else
                                    {
                                        //If not verified, check if next word is
                                        verifyWord(words[nextWordIndex]);

                                        if (verified)
                                        {
                                            //If next word is verified, then keep word type
                                        }
                                        else
                                        {
                                            //If not verified, find matching word type
                                            stored = false;
                                            foreach (WordsScriptableObjects sc in wordsScriptableObjects)
                                            {
                                                foreach (WordsScriptableObjects item in wordsScriptableObjects)
                                                {
                                                    //If sc's after matches with item's before, then store
                                                    if (sc.beforeAfterFormulaType[1] == item.beforeAfterFormulaType[0])
                                                    {
                                                        //Find matching scriptable objects
                                                        if (!stored)
                                                        {
                                                            if (sc.wordTypesEnum == wordTypesEnum[nextWordIndex])
                                                            {
                                                                //If matches with next word's type, get the value before. [0 is before and 1 is after]
                                                                stored = true;
                                                                wordTypesEnum[thisWordIndex] = sc.beforeAfterFormulaType[1];
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                            if (!stored)
                                            {
                                                //If not matches found, cannot decide, debug
                                                consoleDisp.text = consoleDisp.text + " Cannot find any relationship between word type " + wordTypesEnum[prevWordIndex].GetType().GetEnumName(wordTypesEnum[prevWordIndex]) + " and " + wordTypesEnum[nextWordIndex].GetType().GetEnumName(wordTypesEnum[nextWordIndex]) + " >>>" +
"";
                                                if (wordTypesEnum[nextWordIndex] == WordTypesEnum.nothing)
                                                {
                                                    wordTypesEnum[thisWordIndex] = wordTypesEnum[prevWordIndex];
                                                }
                                                else
                                                {
                                                    wordTypesEnum[thisWordIndex] = WordTypesEnum.nothing;
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //If nothing, wait until next repetition...
                                }
                            }
                            else if(thisWordIndex == 0)
                            {
                                //First word, find only 1 word after
                                if (wordTypesEnum[nextWordIndex] != WordTypesEnum.nothing)
                                {
                                    //Check if next word is verified
                                    verifyWord(words[nextWordIndex]);
                                    bool stored = false;

                                    if (verified)
                                    {
                                        //If verified, store this word's type accordingly
                                        foreach (WordsScriptableObjects sc in wordsScriptableObjects)
                                        {
                                            //Find matching scriptable objects
                                            if (!stored)
                                            {
                                                if (sc.wordTypesEnum == wordTypesEnum[nextWordIndex])
                                                {
                                                    //If matches with next word's type, get the value before. [0 is before and 1 is after]
                                                    stored = true;
                                                    wordTypesEnum[thisWordIndex] = sc.beforeAfterFormulaType[0];
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //If not verified, check for any verified words after this one
                                        bool foundVerified = false;
                                        for (int h = nextWordIndex; h < words.Length; h++)
                                        {
                                            int targetVerifiedWordIndex = 0;
                                            //Check for any verified word
                                            verifyWord(words[nextWordIndex]);

                                            if (verified)
                                            {
                                                targetVerifiedWordIndex = h;
                                                foundVerified = true;

                                                //Store each word's type accordingly, from verified one to this word
                                                for (int e = targetVerifiedWordIndex - 1; e >= thisWordIndex; e--)
                                                {
                                                    stored = false;
                                                    foreach (WordsScriptableObjects sc in wordsScriptableObjects)
                                                    {
                                                        //Find matching scriptable objects
                                                        if (!stored)
                                                        {
                                                            if (sc.wordTypesEnum == wordTypesEnum[e + 1])   //e + 1, as value of word after this one
                                                            {
                                                                //If matches with target word's type, get the value before. [0 is before and 1 is after]
                                                                if (e + 1 != targetVerifiedWordIndex)
                                                                {
                                                                    //If below, then compate to make sure of word type
                                                                    if (sc.beforeAfterFormulaType[1] == wordTypesEnum[e + 1])
                                                                    {
                                                                        stored = true;
                                                                        wordTypesEnum[e] = sc.beforeAfterFormulaType[0];
                                                                    }
                                                                    else
                                                                    {
                                                                        //Does not match, do not store
                                                                        stored = false;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    stored = true;
                                                                    wordTypesEnum[e] = sc.beforeAfterFormulaType[0];
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        //If did not find any verified words, simple store this word's type accordingly
                                        if (!foundVerified)
                                        {
                                            stored = false;
                                            foreach (WordsScriptableObjects sc in wordsScriptableObjects)
                                            {
                                                //Find matching scriptable objects
                                                if (!stored)
                                                {
                                                    if (sc.wordTypesEnum == wordTypesEnum[nextWordIndex])
                                                    {
                                                        //If matches with next word's type, get the value before. [0 is before and 1 is after]
                                                        stored = true;
                                                        wordTypesEnum[thisWordIndex] = sc.beforeAfterFormulaType[0];
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //If nothing, wait until next repetition...
                                }
                            }
                            else
                            {
                                //Last Word, find only 1 word before.
                                if (wordTypesEnum[prevWordIndex] != WordTypesEnum.nothing)
                                {
                                    //Check if last word is verified
                                    verifyWord(words[prevWordIndex]);
                                    bool stored = false;

                                    if (verified)
                                    {
                                        //If verified, store this word's type accordingly
                                        foreach (WordsScriptableObjects sc in wordsScriptableObjects)
                                        {
                                            //Find matching scriptable objects
                                            if (!stored)
                                            {
                                                if (sc.wordTypesEnum == wordTypesEnum[prevWordIndex])
                                                {
                                                    //If matches with last word's type, get the value after. [0 is before and 1 is after]
                                                    stored = true;
                                                    consoleDisp.text = consoleDisp.text + " Stored " + words[thisWordIndex] + " as word type " + sc.wordTypesEnum.GetType().GetEnumName(sc.wordTypesEnum) + " >>>" +
    "";
                                                    wordTypesEnum[thisWordIndex] = sc.beforeAfterFormulaType[1];
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //If not verified, check for any verified words before this one
                                        bool foundVerified = false;
                                        for (int h = prevWordIndex; h < words.Length; h++)
                                        {
                                            int targetVerifiedWordIndex = 0;
                                            //Check for any verified word
                                            verifyWord(words[prevWordIndex]);

                                            if (verified)
                                            {
                                                targetVerifiedWordIndex = h;
                                                foundVerified = true;

                                                //Store each word's type accordingly, from verified one to this word
                                                for (int e = thisWordIndex; e < words.Length; e++)
                                                {
                                                    stored = false;
                                                    foreach (WordsScriptableObjects sc in wordsScriptableObjects)
                                                    {
                                                        //Find matching scriptable objects
                                                        if (!stored)
                                                        {
                                                            if (sc.wordTypesEnum == wordTypesEnum[e - 1])   //e + 1, as value of word after this one
                                                            {
                                                                //If matches with target word's type, get the value before. [0 is before and 1 is after]
                                                                if (e - 1 != targetVerifiedWordIndex)
                                                                {
                                                                    //If below, then compate to make sure of word type
                                                                    if (sc.beforeAfterFormulaType[1] == wordTypesEnum[e - 1])
                                                                    {
                                                                        stored = true;
                                                                        wordTypesEnum[e] = sc.beforeAfterFormulaType[0];
                                                                    }
                                                                    else
                                                                    {
                                                                        //Does not match, do not store
                                                                        stored = false;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    stored = true;
                                                                    wordTypesEnum[e] = sc.beforeAfterFormulaType[0];
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        //If did not find any verified words, simple store this word's type accordingly
                                        if (!foundVerified)
                                        {
                                            stored = false;
                                            foreach (WordsScriptableObjects sc in wordsScriptableObjects)
                                            {
                                                //Find matching scriptable objects
                                                if (!stored)
                                                {
                                                    if (sc.wordTypesEnum == wordTypesEnum[prevWordIndex])
                                                    {
                                                        //If matches with next word's type, get the value before. [0 is before and 1 is after]
                                                        stored = true;
                                                        wordTypesEnum[thisWordIndex] = sc.beforeAfterFormulaType[0];
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //If nothing, wait until next repetition...
                                }
                            }
                        }

                        j++;
                    }
                }
                GenerateReport();
            }
        }

        //Finished Training, show report
        trainingTextObject.SetActive(false);
        reportText.gameObject.SetActive(true);
        finishedTraining = true;
    }

    /// <summary>
    /// This method is used to analyse the information given and get the word types, they are stored in <see cref="wordTypesEnum"/>
    /// </summary>
    public void AnalyseInformation()
    {
        //Get Information
        information = trainingTexts.text;
        information = information.Replace("?", ".");
        Debug.Log("Information = " + information);

        //Split by lines
        lines = information.Split("."[0]);

        string[] tempLines = lines;
        lines = new string[lines.Length - 1];

        for (int z = 0; z < lines.Length; z++)
        {
            lines[z] = tempLines[z];
        }

        foreach (string line in lines)
        {
            for (int q = 0; q < perLineRepetition; q++)
            {
                //Repeating n times, to get better results
                int k = 0;
                if (line != "" || line != " ")
                {
                    //Split each words
                    words = line.Split(" "[0]);

                    positions = new int[words.Length];
                    wordTypesEnum = new WordTypesEnum[words.Length];

                    //Identify already known words
                    foreach (string word in words)
                    {
                        Debug.Log("Word = " + word + ", position " + k);
                        //Finding already known words
                        found = false;

                        foreach (string wordItem in nouns)
                        {
                            if (word.ToLower() == wordItem.ToLower())
                            {
                                found = true;
                                Debug.Log(word + " is a noun!");
                                wordTypesEnum[k] = WordTypesEnum.noun;
                            }
                        }

                        if (!found)
                        {
                            foreach (string wordItem in proNouns)
                            {
                                if (word.ToLower() == wordItem.ToLower())
                                {
                                    found = true;
                                    Debug.Log(word + " is a pronoun!");
                                    wordTypesEnum[k] = WordTypesEnum.pronoun;
                                }
                            }
                        }

                        if (!found)
                        {
                            foreach (string wordItem in verbs)
                            {
                                if (word.ToLower() == wordItem.ToLower())
                                {
                                    found = true;
                                    Debug.Log(word + " is a verb!");
                                    wordTypesEnum[k] = WordTypesEnum.verb;
                                }
                            }
                        }

                        if (!found)
                        {
                            foreach (string wordItem in adverbs)
                            {
                                if (word.ToLower() == wordItem.ToLower())
                                {
                                    found = true;
                                    Debug.Log(word + " is an adverbs!");
                                    wordTypesEnum[k] = WordTypesEnum.adverb;
                                }
                            }
                        }

                        if (!found)
                        {
                            foreach (string wordItem in adjectives)
                            {
                                if (word.ToLower() == wordItem.ToLower())
                                {
                                    found = true;
                                    Debug.Log(word + " is an adjective!");
                                    wordTypesEnum[k] = WordTypesEnum.adjective;
                                }
                            }
                        }

                        if (!found)
                        {
                            foreach (string wordItem in prepositions)
                            {
                                if (word.ToLower() == wordItem.ToLower())
                                {
                                    found = true;
                                    Debug.Log(word + " is a preposition!");
                                    wordTypesEnum[k] = WordTypesEnum.preposition;
                                }
                            }
                        }

                        if (!found)
                        {
                            foreach (string wordItem in conjuctions)
                            {
                                if (word.ToLower() == wordItem.ToLower())
                                {
                                    found = true;
                                    Debug.Log(word + " is a conjuction!");
                                    wordTypesEnum[k] = WordTypesEnum.conjuction;
                                }
                            }
                        }

                        k++;
                    }
                }
            }
        }

        Debug.Log("Finished Analysing...");
    }

    /// <summary>
    /// This method is used to analyse the information given and get the word types, they are stored in <see cref="wordTypesEnum"/>
    /// <para><paramref name="data"/> : The data to analyse</para>
    /// </summary>
    /// <param name="data">The data to analyse</param>
    public void AnalyseInformation(string data)
    {
        //Get Information
        information = data;
        information = information.Replace("?", ".");
        Debug.Log("Information = " + information);

        //Split by lines
        lines = information.Split("."[0]);

        string[] tempLines = lines;
        lines = new string[lines.Length - 1];

        for (int z = 0; z < lines.Length; z++)
        {
            lines[z] = tempLines[z];
        }

        foreach (string line in lines)
        {
            for (int q = 0; q < perLineRepetition; q++)
            {
                //Repeating n times, to get better results
                int k = 0;
                if (line != "" || line != " ")
                {
                    //Split each words
                    words = line.Split(" "[0]);

                    positions = new int[words.Length];
                    wordTypesEnum = new WordTypesEnum[words.Length];

                    //Identify already known words
                    foreach (string word in words)
                    {
                        Debug.Log("Word = " + word + ", position " + k);
                        //Finding already known words
                        found = false;

                        foreach (string wordItem in nouns)
                        {
                            if (word.ToLower() == wordItem.ToLower())
                            {
                                found = true;
                                Debug.Log(word + " is a noun!");
                                wordTypesEnum[k] = WordTypesEnum.noun;
                            }
                        }

                        if (!found)
                        {
                            foreach (string wordItem in proNouns)
                            {
                                if (word.ToLower() == wordItem.ToLower())
                                {
                                    found = true;
                                    Debug.Log(word + " is a pronoun!");
                                    wordTypesEnum[k] = WordTypesEnum.pronoun;
                                }
                            }
                        }

                        if (!found)
                        {
                            foreach (string wordItem in verbs)
                            {
                                if (word.ToLower() == wordItem.ToLower())
                                {
                                    found = true;
                                    Debug.Log(word + " is a verb!");
                                    wordTypesEnum[k] = WordTypesEnum.verb;
                                }
                            }
                        }

                        if (!found)
                        {
                            foreach (string wordItem in adverbs)
                            {
                                if (word.ToLower() == wordItem.ToLower())
                                {
                                    found = true;
                                    Debug.Log(word + " is an adverbs!");
                                    wordTypesEnum[k] = WordTypesEnum.adverb;
                                }
                            }
                        }

                        if (!found)
                        {
                            foreach (string wordItem in adjectives)
                            {
                                if (word.ToLower() == wordItem.ToLower())
                                {
                                    found = true;
                                    Debug.Log(word + " is an adjective!");
                                    wordTypesEnum[k] = WordTypesEnum.adjective;
                                }
                            }
                        }

                        if (!found)
                        {
                            foreach (string wordItem in prepositions)
                            {
                                if (word.ToLower() == wordItem.ToLower())
                                {
                                    found = true;
                                    Debug.Log(word + " is a preposition!");
                                    wordTypesEnum[k] = WordTypesEnum.preposition;
                                }
                            }
                        }

                        if (!found)
                        {
                            foreach (string wordItem in conjuctions)
                            {
                                if (word.ToLower() == wordItem.ToLower())
                                {
                                    found = true;
                                    Debug.Log(word + " is a conjuction!");
                                    wordTypesEnum[k] = WordTypesEnum.conjuction;
                                }
                            }
                        }

                        k++;
                    }
                }
            }
        }

        Debug.Log("Finished Analysing...");
    }

    /// <summary>
    /// This method is used to replace words with their respective word types.
    /// </summary>
    /// <param name="showWordsReplaced">Should the printed text include the original words : wordType(Word)</param>
    public void ReplaceWordsWithType(bool showWordsReplaced = true)
    {
        Debug.Log("Called Replace...");
        temp = "";

        information = trainingTexts.text;

        //Split by lines
        lines = information.Split("."[0]);

        string[] tempLines = lines;
        lines = new string[lines.Length - 1];

        for (int z = 0; z < lines.Length; z++)
        {
            lines[z] = tempLines[z];
        }

        foreach (string line in lines)
        {
            Debug.Log("Line entered, = " + line);
            //Split each words
            words = line.Split(" "[0]);

            int k = 0;
            foreach (string word in words)
            {
                Debug.Log("Word = " + word + ", position " + k);
                //Finding already known words
                found = false;

                if (showWordsReplaced)
                {
                    temp = temp + "(" + word + ")";
                }

                foreach (string wordItem in nouns)
                {
                    if (word.ToLower() == wordItem.ToLower())
                    {
                        found = true;
                        temp = temp + "noun ";
                    }
                }

                if (!found)
                {
                    foreach (string wordItem in proNouns)
                    {
                        if (word.ToLower() == wordItem.ToLower())
                        {
                            found = true;
                            temp = temp + "pronoun ";                        }
                    }
                }

                if (!found)
                {
                    foreach (string wordItem in verbs)
                    {
                        if (word.ToLower() == wordItem.ToLower())
                        {
                            found = true;
                            temp = temp + "verb ";
                        }
                    }
                }

                if (!found)
                {
                    foreach (string wordItem in adverbs)
                    {
                        if (word.ToLower() == wordItem.ToLower())
                        {
                            found = true;
                            temp = temp + "adverb ";
                        }
                    }
                }

                if (!found)
                {
                    foreach (string wordItem in adjectives)
                    {
                        if (word.ToLower() == wordItem.ToLower())
                        {
                            found = true;
                            temp = temp + "adjective ";
                        }
                    }
                }

                if (!found)
                {
                    foreach (string wordItem in prepositions)
                    {
                        if (word.ToLower() == wordItem.ToLower())
                        {
                            found = true;
                            temp = temp + "preposition ";
                        }
                    }
                }

                if (!found)
                {
                    foreach (string wordItem in conjuctions)
                    {
                        if (word.ToLower() == wordItem.ToLower())
                        {
                            found = true;
                            temp = temp + "conjuction ";
                        }
                    }
                }

                k++;
            }

            temp = temp + ".";
        }

        trainingTextField.text = temp;
    }

    /// <summary>
    /// This method is used to store informations about frequency on a wordly basis
    /// </summary>
    public void TrainLearnSyntax()
    {
        //Store the original information
        originalInformationInprocessed = trainingTexts.text;

        //Analyse Information
        AnalyseInformation();

        //Show replaced words
        ReplaceWordsWithType(false);

        //Getting replaced words
        int numberOfLines = lines.Length;
        string[] perLineFormula = new string[numberOfLines];
        string[] perLineWords = new string[numberOfLines];

        perLineFormula = temp.Split("."[0]);
        perLineWords = information.Split("."[0]);

        for (int i = 0; i < numberOfLines; i++)
        {
            Debug.Log(perLineWords[i]);
            Debug.Log(perLineFormula[i]);
        }

        //Getting before and after words
        int amntOfUniqueWords = GetAmountOfUniqueWords(originalInformationInprocessed);

        commonPrevWords = new string[amntOfUniqueWords];
        commonAfterWords = new string[amntOfUniqueWords];
        uniqueWords = GetUniqueWords(originalInformationInprocessed);

        linesDebug = originalInformationInprocessed.Split("."[0]);

        foreach (string line in linesDebug)
        {
            //Looping through all the lines
            int q = 0;

            foreach (string word in line.Split(" "[0]))
            {
                if (word != " " && word != "")
                {
                    Debug.Log("Q = " + q + ", array size of (uniqueWords)= " + uniqueWords.Count + ", array size of (commonAfterWords) = " + commonAfterWords.Length + ", array size of (commonPrevWords) = " + commonPrevWords.Length + ", total words = " + line.Split(" "[0]).Length + ", unique word index = " + uniqueWords.IndexOf(word) + ", searching for word = " + word);
                    Debug.Log("Word finding = " + commonAfterWords[uniqueWords.IndexOf(word)] + ", commong prev words = " + commonPrevWords[uniqueWords.IndexOf(word)]);
                    //Looping through all the words
                    if (q == 0)
                    {
                        //First word
                        //Get only 1 word after
                        Debug.Log("Fetching 1 after = " + q + " + 1");
                        string afterWord = line.Split(" "[0])[q + 1];


                        if (wordsAfterCount.ContainsKey(word))
                        {
                            if (wordsAfterCount[word].ContainsKey(afterWord))
                            {
                                GetSetWeight(word, afterWord, wordsAfterCount[word][afterWord] + 1, true);
                            }
                            else
                            {
                                GetSetWeight(word, afterWord, 1, true);
                            }
                        }
                        else
                        {
                            GetSetWeight(word, afterWord, 1, true);
                        }

                        float weigth = wordsAfterCount[word][afterWord];

                        Debug.Log("weight = " + weigth + ", needed to accept = " + afterWordsBias + " word after = " + afterWord);

                        //Checking occurence weight before adding
                        try
                        {
                            Debug.Log("Word to find = " + afterWord + ", to find in =" + commonAfterWords[uniqueWords.IndexOf(word)]);

                            if (commonAfterWords[uniqueWords.IndexOf(word)].Contains(afterWord))
                            {
                                //Contains this word, simply update its weight
                                //Split the groups
                                string[] afterWordsSplit = commonAfterWords[uniqueWords.IndexOf(word)].Split(">"[0]);

                                foreach (string item in afterWordsSplit)
                                {
                                    if (item.Contains(afterWord))
                                    {
                                        //This is the reference we are searching, split and update its weight
                                        commonAfterWords[uniqueWords.IndexOf(word)].Replace(item, afterWord + "|" + weigth);
                                    }
                                }
                            }
                            else
                            {
                                Debug.Log("[DEBUG]:Adding new word");
                                //Does not contain any reference of this word, add this word
                                commonAfterWords[uniqueWords.IndexOf(word)] += weigth >= afterWordsBias ? afterWord + "|" + weigth + ">" : "";
                            }
                        }
                        catch (System.Exception)
                        {
                            Debug.Log("[ERROR]:Error happened");
                            commonAfterWords[uniqueWords.IndexOf(word)] += weigth >= afterWordsBias ? afterWord + "|" + weigth + ">" : "";
                        }
                    }
                    else if (q == line.Split(" "[0]).Length - 1)
                    {
                        //Last word
                        //Get only 1 word before
                        Debug.Log("Fetching 1 before = " + q + " - 1");
                        string wordPrev = line.Split(" "[0])[q - 1];

                        if (wordsBeforeCount.ContainsKey(word))
                        {
                            if (wordsBeforeCount[word].ContainsKey(wordPrev))
                            {
                                GetSetWeight(word, wordPrev, wordsBeforeCount[word][wordPrev] + 1, false);
                            }
                            else
                            {
                                GetSetWeight(word, wordPrev, 1, false);
                            }
                        }
                        else
                        {
                            GetSetWeight(word, wordPrev, 1, false);
                        }

                        float weigth = wordsBeforeCount[word][wordPrev];

                        Debug.Log("weight = " + weigth + ", needed to accept = " + beforeWordsBias + "word previous = " + wordPrev);

                        //Checking occurence weight before adding
                        try
                        {
                            Debug.Log("Word to find = " + wordPrev + ", to find in =" + commonPrevWords[uniqueWords.IndexOf(word)]);

                            if (commonPrevWords[uniqueWords.IndexOf(word)].Contains(wordPrev))
                            {
                                //Contains this word, simply update its weight
                                //Split the groups
                                string[] afterWordsSplit = commonPrevWords[uniqueWords.IndexOf(word)].Split(">"[0]);

                                foreach (string item in afterWordsSplit)
                                {
                                    if (item.Contains(wordPrev))
                                    {
                                        //This is the reference we are searching, split and update its weight
                                        commonPrevWords[uniqueWords.IndexOf(word)].Replace(item, wordPrev + "|" + weigth);
                                    }
                                }
                            }
                            else
                            {
                                Debug.Log("[DEBUG]:Adding new word");
                                //Does not contain any reference of this word, add this word
                                commonPrevWords[uniqueWords.IndexOf(word)] += weigth >= beforeWordsBias ? wordPrev + "|" + weigth + ">" : "";
                            }
                        }
                        catch (System.Exception)
                        {
                            Debug.Log("[ERROR]:Error happened");
                            commonPrevWords[uniqueWords.IndexOf(word)] += weigth >= beforeWordsBias ? wordPrev + "|" + weigth + ">" : "";
                        }
                    }
                    else
                    {
                        //Middle words
                        //Get 1 word after and before
                        //Before
                        string wordPrev = line.Split(" "[0])[q - 1];

                        if (wordsBeforeCount.ContainsKey(word))
                        {
                            if (wordsBeforeCount[word].ContainsKey(wordPrev))
                            {
                                GetSetWeight(word, wordPrev, wordsBeforeCount[word][wordPrev] + 1, false);
                            }
                            else
                            {
                                GetSetWeight(word, wordPrev, 1, false);
                            }
                        }
                        else
                        {
                            GetSetWeight(word, wordPrev, 1, false);
                        }

                        float weigth = wordsBeforeCount[word][wordPrev];

                        Debug.Log("weight = " + weigth + ", needed to accept = " + beforeWordsBias + "word previous = " + wordPrev);

                        //Checking occurence weight before adding
                        try
                        {
                            Debug.Log("If statement entered, output = " + (commonPrevWords.Length > 0 || !commonPrevWords[uniqueWords.IndexOf(word)].Contains(wordPrev)));
                            Debug.Log("Word to find = " + wordPrev + ", to find in =" + commonPrevWords[uniqueWords.IndexOf(word)]);

                            if (commonPrevWords[uniqueWords.IndexOf(word)].Contains(wordPrev))
                            {
                                //Contains this word, simply update its weight
                                //Split the groups
                                string[] afterWordsSplit = commonPrevWords[uniqueWords.IndexOf(word)].Split(">"[0]);

                                foreach (string item in afterWordsSplit)
                                {
                                    if (item.Contains(wordPrev))
                                    {
                                        //This is the reference we are searching, split and update its weight
                                        commonPrevWords[uniqueWords.IndexOf(word)].Replace(item, wordPrev + "|" + weigth);
                                    }
                                }
                            }
                            else
                            {
                                Debug.Log("[DEBUG]:Adding new word");
                                //Does not contain any reference of this word, add this word
                                commonPrevWords[uniqueWords.IndexOf(word)] += weigth >= beforeWordsBias ? wordPrev + "|" + weigth + ">" : "";
                            }
                        }
                        catch (System.Exception)
                        {
                            Debug.Log("[ERROR]:Error happened, most probably commonPrevWords is empty");
                            commonPrevWords[uniqueWords.IndexOf(word)] += weigth >= beforeWordsBias ? wordPrev + "|" + weigth + ">" : "";
                        }

                        //After
                        Debug.Log("Fetching 1 after = " + q + " + 1");
                        string afterWord = line.Split(" "[0])[q + 1];

                        if (wordsAfterCount.ContainsKey(word))
                        {
                            if (wordsAfterCount[word].ContainsKey(afterWord))
                            {
                                GetSetWeight(word, afterWord, wordsAfterCount[word][afterWord] + 1, true);
                            }
                            else
                            {
                                GetSetWeight(word, afterWord, 1, true);
                            }
                        }
                        else
                        {
                            GetSetWeight(word, afterWord, 1, true);
                        }

                        weigth = wordsAfterCount[word][afterWord];

                        Debug.Log("weight = " + weigth + ", needed to accept = " + afterWordsBias + " word after = " + afterWord);

                        //Checking occurence weight before adding
                        try
                        {
                            Debug.Log("Word to find = " + afterWord + ", to find in =" + commonAfterWords[uniqueWords.IndexOf(word)]);

                            if (commonAfterWords[uniqueWords.IndexOf(word)].Contains(afterWord))
                            {
                                //Contains this word, simply update its weight
                                //Split the groups
                                string[] afterWordsSplit = commonAfterWords[uniqueWords.IndexOf(word)].Split(">"[0]);

                                foreach (string item in afterWordsSplit)
                                {
                                    if (item.Contains(afterWord))
                                    {
                                        //This is the reference we are searching, split and update its weight
                                        commonAfterWords[uniqueWords.IndexOf(word)].Replace(item, afterWord + "|" + weigth);
                                    }
                                }
                            }
                            else
                            {
                                Debug.Log("[DEBUG]:Adding new word");
                                //Does not contain any reference of this word, add this word
                                commonAfterWords[uniqueWords.IndexOf(word)] += weigth >= afterWordsBias ? afterWord + "|" + weigth + ">" : "";
                            }
                        }
                        catch (System.Exception)
                        {
                            Debug.Log("[ERROR]:Error happened");
                            commonAfterWords[uniqueWords.IndexOf(word)] += weigth >= afterWordsBias ? afterWord + "|" + weigth + ">" : "";
                        }
                    }
                    q++;
                }
            }
        }

        //Get occurences, whole text
        foreach (string item in perLineWords)
        {
            if (item != "" && item != " ")
            {
                GetPerLineOccurence(item);
            }
        }

        //Calculating mean values
        Debug.Log("Occurences of each words = ");
        foreach (string key in wordOccurence.Keys)
        {
            float meanOccurence = Mathf.Round((int)wordOccurence[key] / (wordOccurence.Count + 1));
            float meanOccurencePerLine = Mathf.Round((int)wordOccurence[key] / ((wordOccurence.Count + 1) / (perLineWords.Length - 1)));

            Debug.Log(key + " appeared " + (int)wordOccurence[key]);
            Debug.Log(key + (string.Format(" has a mean occurence value of {0} out of {1} words in the whole text.", meanOccurence, (wordOccurence.Count + 1))));
            Debug.Log(key + (string.Format(" has a mean occurence value of {0} per line, out of {1} lines.", meanOccurencePerLine, perLineWords.Length - 1)));

            if (wordHash.Contains(key))
            {
                Debug.Log("Hashtable already contains key " + key + ", updating it.");
                string wor = (string)wordHash[key];
                float meanOcc = float.Parse(wor.Split(","[0])[0]);
                float meanOccPerLine = float.Parse(wor.Split(","[0])[1]);

                meanOcc += meanOccurence;
                meanOccPerLine += meanOccurencePerLine;

                Dictionary<string, string> commonAfterWordsList = new Dictionary<string, string>();
                Dictionary <string, string> commonPrevWordsList = new Dictionary<string, string>();

                foreach (string item in commonAfterWords)
                {
                    try
                    {
                        if (item != "" && item != " " && item.Contains("|"))
                        {
                            Debug.Log("Adding item to list = " + item);
                            commonAfterWordsList.Add(item.ToLower().Split("|"[0])[0], item.ToLower());
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                foreach (string item in commonPrevWords)
                {
                    try
                    {
                        if (item != "" && item != " " && item.Contains("|"))
                        {
                            Debug.Log("Adding item to list = " + item);
                            commonPrevWordsList.Add(item.ToLower().Split("|"[0])[0], item.ToLower());
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                if (commonAfterWordsList.ContainsKey(key.ToLower()))
                {
                    if (commonPrevWordsList.ContainsKey(key.ToLower()))
                    {
                        wor = meanOcc + "," + meanOccPerLine + "," + commonPrevWordsList[key.ToLower()] + "," + commonAfterWordsList[key.ToLower()];
                    }
                    else
                    {
                        //If does not contain definition, make one
                        commonPrevWordsList.Add(key.ToLower(), "");

                        wor = meanOcc + "," + meanOccPerLine + "," + commonPrevWordsList[key.ToLower()] + "," + commonAfterWordsList[key.ToLower()];
                    }
                }
                else
                {
                    if (commonPrevWordsList.ContainsKey(key.ToLower()))
                    {
                        //If does not contain definition, make one
                        commonAfterWordsList.Add(key.ToLower(), "");

                        wor = meanOcc + "," + meanOccPerLine + "," + commonPrevWordsList[key.ToLower()] + "," + commonAfterWordsList[key.ToLower()];
                    }
                    else
                    {
                        //If does not contain definition, make one
                        commonAfterWordsList.Add(key.ToLower(), "");
                        commonPrevWordsList.Add(key.ToLower(), "");

                        wor = meanOcc + "," + meanOccPerLine + "," + commonPrevWordsList[key.ToLower()] + "," + commonAfterWordsList[key.ToLower()];
                    }
                }

                //Saving changes
                List<string> tempStrList = new List<string>();

                foreach (string item in commonAfterWordsList.Values)
                {
                    tempStrList.Add(item);
                }

                commonAfterWords = tempStrList.ToArray();

                tempStrList = new List<string>();

                foreach (string item in commonPrevWordsList.Values)
                {
                    tempStrList.Add(item);
                }

                commonPrevWords = tempStrList.ToArray();

                /*
                if (commonPrevWords.Length < uniqueWords.IndexOf(key)) 
                {
                    if (commonAfterWords.Length < uniqueWords.IndexOf(key))
                    {
                        wor = meanOcc + "," + meanOccPerLine + "," + commonPrevWords[uniqueWords.IndexOf(key)] + "," + commonAfterWords[uniqueWords.IndexOf(key)];
                    }
                    else
                    {
                        Debug.Log("Debug : ");
                        Debug.Log("Index to find = " + uniqueWords.IndexOf(key));
                        Debug.Log("Size of commonAfterWords = " + commonAfterWords.Length);
                        Debug.Log("Size of commonPrevWords = " + commonPrevWords.Length);
                        //Debug.Log("Value commonAfterWords = " + commonAfterWords[uniqueWords.IndexOf(key)]);
                        //Debug.Log("Value commonPrevWords = " + commonPrevWords[uniqueWords.IndexOf(key)]);
                        //Debug.Log("Value commonAfterWords (1 before) = " + commonAfterWords[uniqueWords.IndexOf(key) - 1]);
                        //Debug.Log("Value = " + commonPrevWords[uniqueWords.IndexOf(key)]);
                        wor = meanOcc + "," + meanOccPerLine + "," + commonPrevWords[uniqueWords.IndexOf(key)] + "," + "";
                    }
                }
                else
                {
                    if (commonAfterWords.Length < uniqueWords.IndexOf(key))
                    {
                        wor = meanOcc + "," + meanOccPerLine + "," + commonPrevWords[uniqueWords.IndexOf(key)] + "," + commonAfterWords[uniqueWords.IndexOf(key)];
                    }
                    else
                    {
                        Debug.Log("Debug : ");
                        Debug.Log("Index to find = " + uniqueWords.IndexOf(key));
                        Debug.Log("Size of commonAfterWords = " + commonAfterWords.Length);
                        Debug.Log("Size of commonPrevWords = " + commonPrevWords.Length);
                       // Debug.Log("Value commonAfterWords = " + commonAfterWords[uniqueWords.IndexOf(key)]);
                        //Debug.Log("Value commonPrevWords = " + commonPrevWords[uniqueWords.IndexOf(key)]);
                        //Debug.Log("Value commonAfterWords (1 before) = " + commonAfterWords[uniqueWords.IndexOf(key) - 1]);
                        //Debug.Log("Value = " + commonPrevWords[uniqueWords.IndexOf(key)]);
                        wor = meanOcc + "," + meanOccPerLine + "," + "" + "," + "";
                    }

                }
                */
                wordHash[key] = wor;
            }
            else
            {
                Debug.Log("Hashtable does not contain key " + key + ", adding it.");
                wordHash.Add(key, meanOccurence + "," + meanOccurencePerLine + "," + commonPrevWords[uniqueWords.IndexOf(key)] + "," + commonAfterWords[uniqueWords.IndexOf(key)]);
            }
            Debug.Log("Added to hashtable with key = " + key);
            Debug.Log("Added to hashtable = " + wordHash[key]);
        }

        knownWordsDetails.fullWordsDetails = new string[wordHash.Count];

        int jj = 0;
        Debug.Log("Entering foreach..." + wordHash.Count);
        foreach (string key in wordHash.Keys)
        {
            knownWordsDetails.fullWordsDetails[jj] = key + ":" + (string)wordHash[key];
            Debug.Log(">>" + knownWordsDetails.fullWordsDetails[jj]);
            jj++;
        }
    }   //Completed, this method is for storing informations about know words.

    /// <summary>
    /// Used to make the AI learn about the syntax of questions
    /// </summary>
    public void LearnQuestionSyntax()
    {
        string rawData = trainingTexts.text;

        foreach (string item in Enum.GetNames(typeof(Questions)))
        {
            Debug.Log("Question = " + item);
            trainingTexts.text.Replace(item, "");
        }

        AnalyseInformation();

        string processedInformation = "";
        for (int i = 0; i < wordTypesEnum.Length; i++)
        {
            Debug.Log("Adding " + Enum.GetName(typeof(WordTypesEnum), wordTypesEnum[i]));
            processedInformation += Enum.GetName(typeof(WordTypesEnum), wordTypesEnum[i]) + " ";
        }

        GetPerTextOccurence(trainingTexts.text.Replace("?", "."), true);

        Debug.Log("Calling question...");
        question.SetQuestionSyntax(rawData, processedInformation, wordOccurenceDict);
    }   //Used to learn how questions are formed

    public void AskQuestion()
    {
        AskQuestion(trainingTexts.text);
    }

    /// <summary>
    /// Used to make the AI formulate a question based on provided texts
    /// <para><paramref name="data"/>: The information the AI needs to ask a question.</para>
    /// </summary>
    /// <param name="data">The information the AI needs to ask a question on</param>
    public void AskQuestion(string data)
    {
        /*
         * Get a question descriptor \/
         * Analyse the data \/
         * Get nouns and verbs \/
         * Formulate Question
         * Verify Integrity
         */

        //Finding a question descriptor
        Questions tempQuestionDescriptor = (Questions)UnityEngine.Random.Range(0, 5);
        //Questions tempQuestionDescriptor = Questions.;
        Debug.Log("Question Descriptor = " + tempQuestionDescriptor);

        float questionWeight = 0;

        //Analysing the data
        wordTypesEnum = new WordTypesEnum[0];    //<--- Analysed data will be stored in this array

        string[] wordsInData = new string[0];

        wordsInData = data.Replace(" .", "").Split(" "[0]);    //<---- Stores all words in the given data

        AnalyseInformation(data);

        //Formulating Question
        /*
         * Choose question desctiptor,
         * Get a list of most common after words, sorted by weight
         * Find a list of most common words after available for use in the given data, sorted by weight
         * Get a list of word types after the most common after word and repeat, weight should be considered
         */

        string questionSyntaxForChosenWordType = loadedQuestion.questionSyntaxData[Enum.GetName(typeof(Questions), tempQuestionDescriptor)];
        Debug.Log("Question Syntax for " + Enum.GetName(typeof(Questions), tempQuestionDescriptor) + " : " + questionSyntaxForChosenWordType);

        List<string> wordsAfterQuestionDescriptor = new List<string>();

        foreach (string item in questionSyntaxForChosenWordType.Split(","[0]))
        {
            if (item != "" && item != " " && item != null)
            {
                Debug.Log("Adding " + item);
                wordsAfterQuestionDescriptor.Add(item);
            }
        }       //<---- Getting a list of word afters for the question descriptor

        /*
         * Question Syntax Mapping
         * QuestionDescriptor word(wordType)(weight)(pos) word(wordType)(weight)(pos)   <--- Stored as QSyntax for this example
         * QuestionDescriptor = QSyntax.Split(" "[0])[0];
         * QuestionDescriptor_wordAfter = QSyntax.Split(" "[0])[1].Split("("[0])[0];
         */

        List<string> filteredQSyntax = new List<string>();      //<--- Contains a list of Question Syntaxes only for the specified Question Descriptor
        List<float> fileteredQSyntaxWeights = new List<float>();

        foreach (Question.QuestionSyntax QSyntax in loadedQuestion.loadedQuestionSyntaxes)
        {
            if (QSyntax.question == tempQuestionDescriptor)
            {
                Debug.Log("(2) : QSyntax.question = " + QSyntax.question);
                //Debug.Log("(2) : QSyntax = " + QSyntax.questionSyntax[0]);

                string tempJointSyntax = "";

                try
                {
                    foreach (string item in QSyntax.questionSyntax)
                    {
                        tempJointSyntax += item + " ";
                    }
                    Debug.Log("tempJointSyntax = " + tempJointSyntax);
                }
                catch (Exception)
                {
                }

                filteredQSyntax.Add(tempJointSyntax);      //<--- Storing a list of question syntaxes
                fileteredQSyntaxWeights.Add(QSyntax.questionWeightThreshold);      //<--- Storing a list of question syntaxes
            }
        }       //<--- Filtering the list

        //Search for most common after word for the given question descriptor that is also present in the given data
        bool foundNextWord = false;
        string commonAfterWord = "";

        foreach (string commonAfterWords in wordsAfterQuestionDescriptor)       //<--- Looping through most common words after for QDescriptor
        {
            Debug.Log("commonAfterWords = " + commonAfterWords);
            if (!foundNextWord)
            {
                List<string> tEmp = new List<string>();
                foreach (string wordInData in wordsInData)      //<--- Looping through all words in data
                {
                    tEmp.Add(wordInData);
                    Debug.Log("WordInData " + wordInData);
                    if (commonAfterWord != "" && commonAfterWord != " " && wordInData != "" && wordInData != " ")
                    {
                        Debug.Log("Not Empty");
                        if (commonAfterWords.Replace(" ", "").ToLower() == wordInData.Replace(" ", "").ToLower())
                        {
                            Debug.Log("Found common after word = " + wordInData);
                            commonAfterWord = wordInData;
                            foundNextWord = true;
                        }
                    }
                }

                if (!foundNextWord)
                {
                    if (tEmp.Contains(commonAfterWords))
                    {
                        Debug.Log("(2) : Found common after word = " + commonAfterWords);
                        commonAfterWord = commonAfterWords;
                        foundNextWord = true;
                    }
                }
            }
        }

        List<float> finalWeights = new List<float>();
        if (foundNextWord)
        {
            Dictionary<int, List<string>> wordTypesInList = new Dictionary<int, List<string>>();
            int countTemp = 0;

            foreach (string QSyntax in filteredQSyntax)
            {
                Debug.Log("QSyntax = " + QSyntax);
                string[] tempSyntaxWords = QSyntax.Split(" "[0]);
                List<string> tempSyntaxList = new List<string>();

                foreach (string wordSyntax in tempSyntaxWords)
                {
                    if (wordSyntax != "" && wordSyntax != " ")
                    {
                        Debug.Log("Adding " + wordSyntax + " of " + tempSyntaxWords);
                        tempSyntaxList.Add(wordSyntax.ToLower().Replace(" ", ""));
                    }
                }

                Debug.Log("wordSyntax = " + QSyntax);
                Debug.Log("commonAfterWord = " + commonAfterWord.ToLower());
                bool containsCommonAfterWord = false;

                foreach (string item in tempSyntaxList)
                {
                    Debug.Log("Checking tempSyntaxList = " + item);

                    containsCommonAfterWord = !containsCommonAfterWord ? item.ToLower().Contains(commonAfterWord.ToLower().Replace(" ", "")) : true;
                    Debug.Log("Checking common after words = " + containsCommonAfterWord);
                }

                Debug.Log("Check if syntax contains commonAfterWord " + tempSyntaxList.Contains(commonAfterWord.ToLower().Replace(" ", "")));
                if (containsCommonAfterWord)
                {
                    foreach (string wordSyntax in tempSyntaxList)
                    {
                        string tempWordSyntax = "";
                        tempWordSyntax = wordSyntax.Replace("(", ":");
                        tempWordSyntax = tempWordSyntax.Replace(")", ":");
                        Debug.Log("tempWordSyntax = " + tempWordSyntax);
                        //tempWordSyntax.Split(":"[0])[1]

                        if (wordTypesInList.ContainsKey(countTemp))
                        {
                            wordTypesInList[countTemp].Add(tempWordSyntax.Split(":"[0])[1]);       //<--- Getting only the word type
                        }
                        else
                        {
                            wordTypesInList.Add(countTemp, new List<string>());       //<--- Getting only the word type
                            wordTypesInList[countTemp].Add(tempWordSyntax.Split(":"[0])[1]);        //<--- Getting only the word type
                        }

                        finalWeights.Add(fileteredQSyntaxWeights[countTemp]);

                        Debug.Log("wordTypesInList = " + tempWordSyntax.Split(":"[0])[1]);
                    }
                }

                countTemp++;
            }

            int selection = UnityEngine.Random.Range(0, wordTypesInList.Count - 1);     //<--- Chosing a random syntax
            List<string> chosenSyntax = new List<string>();
            Debug.Log("Chosing Syntax (Random) : max = " + (countTemp - 2) + ", selection = " + selection + " : (original max) = " + wordTypesInList.Count);
            chosenSyntax = wordTypesInList[selection];
            float weightForCurrentSyntax = 0;

            try
            {
                weightForCurrentSyntax = finalWeights[selection];
            }
            catch (Exception)
            {

                weightForCurrentSyntax = 0;
            }

            Debug.Log(chosenSyntax[0]);
            Debug.Log(chosenSyntax.Count);
            chosenSyntax.Remove(chosenSyntax[0]);

            foreach (string item in wordTypesDictionary.Keys)
            {
                Debug.Log("Word Types Dict = " + item);
            }

            List<string> tempChosenSyntax = new List<string>();
            foreach (string item in chosenSyntax)
            {
                Debug.Log("Checking for changes = " + item);
                if (item[item.Length - 1] != "s"[0])
                {
                    tempChosenSyntax.Add(item + "s");
                }
                else
                {
                    tempChosenSyntax.Add(item);
                }
            }

            chosenSyntax = tempChosenSyntax;

            string finalQuestion = Enum.GetName(typeof(Questions), tempQuestionDescriptor) + " " + commonAfterWord.Replace(" ", "");
            Debug.Log(finalQuestion);
            string lastWordInQuestion;
            foreach (string syntaxChosen in chosenSyntax)
            {
                if (syntaxChosen != "" && syntaxChosen != " ")
                {
                    Debug.Log("syntaxChosen = " + syntaxChosen);
                    foreach (string wordData in wordsInData)
                    {
                        Debug.Log("wordData = " + wordData);
                        if (wordTypesDictionary[syntaxChosen.ToLower()].Contains(wordData))
                        {
                            finalQuestion += " " + wordData;
                            Debug.Log(wordData);
                        }
                    }
                }
            }       //<--- Looping through all word types, replace with words
            Debug.Log("?");
            finalQuestion += "?";
            Debug.Log("Final Question = " + finalQuestion);

            Question tempQuestion = new Question();
            tempQuestion.VerifyQuestionIntegrity(finalQuestion.Replace(Enum.GetName(typeof(Questions), tempQuestionDescriptor), "").Replace("?", ""), wordOccurenceDict);

            Debug.Log("Question Weight = " + tempQuestion.questionWeight);
            Debug.Log("Is Question Valid? " + ((tempQuestion.questionWeight >= weightForCurrentSyntax - questionWeightBias) || (tempQuestion.questionWeight <= weightForCurrentSyntax + questionWeightBias)));

            loggedList.Add("Given data = " + data + " : Question formed = " + finalQuestion);

        }
        else
        {
            Debug.Log("Cannot find any common after words... Cannot formulate any questions!");
        }
    }  //Used to make the AI formulate a question based on provided texts

    private void OnApplicationQuit()
    {
        if (LogQuestionsForTraining)
        {
            string AllLog = "";

            foreach (string item in loggedList)
            {
                AllLog += item + ", ";
            }

            File.WriteAllText(pathToSave, AllLog);
            Debug.Log("Logged!");
        }
    }

    /// <summary>
    /// This method is used to load the word details from the brain file.
    /// </summary>
    public void LoadKnownWordDetails()
    {
        if (knownWordsDetails.fullWordsDetails != null)
        {
            if (knownWordsDetails.fullWordsDetails.Length > 0)
            {
                foreach (string item in knownWordsDetails.fullWordsDetails)
                {
                    string key = item.Split(":"[0])[0];
                    string value = item.Split(":"[0])[1];

                    Debug.Log("Key = " + key + ", Value = " + value);
                    wordHash.Add(key, value);

                    //Words after count
                    string[] after = value.Split(","[0])[3].Split(">"[0]);
                    foreach (string word in after)
                    {
                        if (word != "" && word != " ")
                        {
                            //Updating commonprev and commonafter words
                            string afterWord = word.Split("|"[0])[0];
                            Debug.Log("afterWord = " + afterWord + ", word = " + word);
                            float afterWeight = float.Parse(word.Split("|"[0])[1]);

                            Dictionary<string, float> valueDict = new Dictionary<string, float>();

                            valueDict.Add(afterWord, afterWeight);

                            //wordsAfterCount.Add(key, valueDict);
                            wordsAfterCount[key] = valueDict;
                        }
                    }

                    //Word before count
                    string[] prev = value.Split(","[0])[2].Split(">"[0]);
                    foreach (string wordPre in prev)
                    {
                        if (wordPre != "" && wordPre != " ")
                        {
                            //Updating commonprev and commonafter words
                            string prevWord = wordPre.Split("|"[0])[0];
                            float prevWeight = float.Parse(wordPre.Split("|"[0])[1]);

                            Dictionary<string, float> valueDict = new Dictionary<string, float>();

                            valueDict.Add(prevWord, prevWeight);

                            //wordsBeforeCount.Add(key, valueDict);
                            wordsBeforeCount[key] = valueDict;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// This method is used to calculate the amount of times words in a line has appeared, then stores the result in <see cref="wordOccurence"/>
    /// <para><paramref name="line"/>: The line to calculate.</para>
    /// </summary>
    /// <param name="line">The line to calculate</param>
    public void GetPerLineOccurence(string line)
    {
        foreach (string item in line.Split(" "[0]))
        {
            if (!wordOccurence.ContainsKey(item))
            {
                wordOccurence.Add(item, 1);
            }
            else
            {
                wordOccurence[item] = (int)wordOccurence[item] + 1;
            }
        }
    }

    /// <summary>
    /// This method is used to calculate the amount of times words in a line has appeared, then stores the result in <see cref="wordOccurence"/> or if <paramref name="useDictionaryMode"/> is true, stored in <see cref="wordOccurenceDict"/>
    /// <para><paramref name="text"/>: The text to calculate.</para>
    /// <para><paramref name="useDictionaryMode"/>: Should we store the information in a dictionary format instead? <see cref="wordOccurenceDict"/> instead of <see cref="wordOccurence"/>.</para>
    /// </summary>
    /// <param name="text">The text to calculate.</param>
    /// <param name="useDictionaryMode">Should we store the information in a dictionary format instead? <see cref="wordOccurenceDict"/> instead of <see cref="wordOccurence"/>.</param>
    public void GetPerTextOccurence(string text, bool useDictionaryMode)
    {
        Debug.Log("Lines = " + text);
        string[] lines = text.Split("."[0]);

        foreach (string line in lines)
        {
            foreach (string item in line.Split(" "[0]))
            {
                if (item != " " && item != "")
                {
                    if (!wordOccurence.ContainsKey(item))
                    {
                        if (useDictionaryMode)
                        {
                            Debug.Log("(Getting Occurences): Adding " + item);
                            if (!wordOccurenceDict.ContainsKey(item.Replace(" ", ""))) 
                            {
                                wordOccurenceDict.Add(item.Replace(" ", ""), 1);
                            }
                            else
                            {
                                wordOccurenceDict[item.Replace(" ", "")] = 1;
                            }
                        }
                        else
                        {
                            wordOccurence.Add(item.Replace(" ", ""), 1);
                        }
                    }
                    else
                    {
                        if (useDictionaryMode)
                        {
                            Debug.Log("(Getting Occurences): Updating item = " + item + ", value = " + wordOccurenceDict[item] + 1);
                            wordOccurenceDict[item.Replace(" ", "")] = wordOccurenceDict[item.Replace(" ", "")] + 1;
                        }
                        else
                        {
                            wordOccurence[item.Replace(" ", "")] = (int)wordOccurence[item.Replace(" ", "")] + 1;
                        }
                    }
                }
            }
        }

        Debug.Log("Finished finding occurence");
    }

    /// <summary>
    /// Get the amount of unique words in the provided data.
    /// <para><paramref name="data"/>: The data to calculate from.</para>
    /// </summary>
    /// <param name="data">The data to calculate from</param>
    /// <returns>The amount of unique words</returns>
    public int GetAmountOfUniqueWords(string data)
    {
        List<string> processedWords = new List<string>();
        data = data.Replace(".", " ");
        int amntOfUniqueWords = 0;

        foreach (string word in data.Split(" "[0]))
        {
            if (!processedWords.Contains(word) && word != " " && word != "")
            {
                //isUnique, increment and add to list
                amntOfUniqueWords++;
                processedWords.Add(word);
            }
        }

        Debug.Log("Amount of unique words = " + amntOfUniqueWords);
        return amntOfUniqueWords;
    }

    /// <summary>
    /// Gets all the unique words in the data provided.
    /// <para><paramref name="data"/>: The data to search.</para>
    /// </summary>
    /// <param name="data">The data to search</param>
    /// <returns>A list of unique words</returns>
    public List<string> GetUniqueWords(string data)
    {
        List<string> uniqueWords = new List<string>();
        data = data.Replace(".", " ");

        foreach (string word in data.Split(" "[0]))
        {
            if (!uniqueWords.Contains(word) && word != " " && word != "")
            {
                //isUnique, increment and add to list
                uniqueWords.Add(word.Contains(".") ? word.Replace(".", "") : word);
            }
        }

        int count = 0;
        foreach (string item in uniqueWords)
        {
            Debug.Log("Unique word = " + item + ", count = " + count);
            count++;
        }

        return uniqueWords;
    }

    /// <summary>
    /// Generates a report to show the user what the AI has leant
    /// </summary>
    public void GenerateReport()
    {
        for (int i = 0; i < words.Length; i++)
        {
            if (!allWords.Contains(words[i]))
            {
                if (wordTypesEnum[i] == WordTypesEnum.noun)
                {
                    NounsText.text = NounsText.text + words[i] + ", " +
                        "";

                    if (tempWordTypes.nouns.Length > 0)
                    {
                        string[] temp = tempWordTypes.nouns;
                        tempWordTypes.nouns = new string[tempWordTypes.nouns.Length + 1];

                        int j = 0;
                        for (j = 0; j < temp.Length; j++)
                        {
                            Debug.Log("Temp words for noun = " + temp[j]);
                            tempWordTypes.nouns[j] = temp[j];
                        }

                        tempWordTypes.nouns[j] = words[i];
                    }
                    else
                    {
                        tempWordTypes.nouns = new string[1];
                        tempWordTypes.nouns[0] = words[i];
                    }
                }

                if (wordTypesEnum[i] == WordTypesEnum.pronoun)
                {
                    PronounsText.text = PronounsText.text + words[i] + ", " +
                                "";

                    if (tempWordTypes.proNouns.Length > 0)
                    {
                        string[] temp = tempWordTypes.proNouns;
                        tempWordTypes.proNouns = new string[tempWordTypes.proNouns.Length + 1];

                        int j = 0;
                        for (j = 0; j < temp.Length; j++)
                        {
                            tempWordTypes.proNouns[j] = temp[j];
                        }

                        tempWordTypes.proNouns[j] = words[i];
                    }
                    else
                    {
                        tempWordTypes.proNouns = new string[1];
                        tempWordTypes.proNouns[0] = words[i];
                    }
                }

                if (wordTypesEnum[i] == WordTypesEnum.verb)
                {
                    VerbsText.text = VerbsText.text + words[i] + ", " +
                                "";

                    if (tempWordTypes.verbs.Length > 0)
                    {
                        string[] temp = tempWordTypes.verbs;
                        tempWordTypes.verbs = new string[tempWordTypes.verbs.Length + 1];

                        int j = 0;
                        for (j = 0; j < temp.Length; j++)
                        {
                            tempWordTypes.verbs[j] = temp[j];
                        }

                        tempWordTypes.verbs[j] = words[i];
                    }
                    else
                    {
                        tempWordTypes.verbs = new string[1];
                        tempWordTypes.verbs[0] = words[i];
                    }
                }

                if (wordTypesEnum[i] == WordTypesEnum.adverb)
                {
                    AdverbsText.text = AdverbsText.text + words[i] + ", " +
                                "";

                    if (tempWordTypes.adverbs.Length > 0)
                    {
                        string[] temp = tempWordTypes.adverbs;
                        tempWordTypes.adverbs = new string[tempWordTypes.adverbs.Length + 1];

                        int j = 0;
                        for (j = 0; j < temp.Length; j++)
                        {
                            tempWordTypes.adverbs[j] = temp[j];
                        }

                        tempWordTypes.adverbs[j] = words[i];
                    }
                    else
                    {
                        tempWordTypes.adverbs = new string[1];
                        tempWordTypes.adverbs[0] = words[i];
                    }
                }

                if (wordTypesEnum[i] == WordTypesEnum.adjective)
                {
                    AdjectivesText.text = AdjectivesText.text + words[i] + ", " +
                                "";

                    if (tempWordTypes.adjectives.Length > 0)
                    {
                        string[] temp = tempWordTypes.adjectives;
                        tempWordTypes.adjectives = new string[tempWordTypes.adjectives.Length + 1];

                        int j = 0;
                        for (j = 0; j < temp.Length; j++)
                        {
                            tempWordTypes.adjectives[j] = temp[j];
                        }

                        tempWordTypes.adjectives[j] = words[i];
                    }
                    else
                    {
                        tempWordTypes.adjectives = new string[1];
                        tempWordTypes.adjectives[0] = words[i];
                    }
                }

                if (wordTypesEnum[i] == WordTypesEnum.preposition)
                {
                    PrepositionsText.text = PrepositionsText.text + words[i] + ", " +
                                "";

                    if (tempWordTypes.prepositions.Length > 0)
                    {
                        string[] temp = tempWordTypes.prepositions;
                        tempWordTypes.prepositions = new string[tempWordTypes.prepositions.Length + 1];

                        int j = 0;
                        for (j = 0; j < temp.Length; j++)
                        {
                            tempWordTypes.prepositions[j] = temp[j];
                        }

                        tempWordTypes.prepositions[j] = words[i];
                    }
                    else
                    {
                        tempWordTypes.prepositions = new string[1];
                        tempWordTypes.prepositions[0] = words[i];
                    }
                }

                if (wordTypesEnum[i] == WordTypesEnum.conjuction)
                {
                    ConjunctionsText.text = ConjunctionsText.text + words[i] + ", " +
                                "";

                    if (tempWordTypes.conjuctions.Length > 0)
                    {
                        string[] temp = tempWordTypes.conjuctions;
                        tempWordTypes.conjuctions = new string[tempWordTypes.conjuctions.Length + 1];

                        int j = 0;
                        for (j = 0; j < temp.Length; j++)
                        {
                            tempWordTypes.conjuctions[j] = temp[j];
                        }

                        tempWordTypes.conjuctions[j] = words[i];
                    }
                    else
                    {
                        tempWordTypes.conjuctions = new string[1];
                        tempWordTypes.conjuctions[0] = words[i];
                    }
                }

                if (wordTypesEnum[i] == WordTypesEnum.nothing)
                {
                    NothingText.text = NothingText.text + words[i] + ", " +
                        "";
                }

                allWords = allWords + words[i];
            }
        }
    }

    /// <summary>
    /// Used to verify a word if known or not from brain file, value <see cref="verified"/> will be reset to false and be ovveritten if <paramref name="wordsToFind"/> is verified.
    /// <para><paramref name="wordsToFind"/>: The word to verify.</para>
    /// </summary>
    /// <param name="wordsToFind">The word to verify</param>
    public void verifyWord(string wordsToFind)
    {
        verified = false;

        foreach (string wordItem in nouns)
        {
            if (wordsToFind.ToLower() == wordItem.ToLower())
            {
                verified = true;
            }
        }

        if (!verified)
        {
            foreach (string wordItem in proNouns)
            {
                if (wordsToFind.ToLower() == wordItem.ToLower())
                {
                    verified = true;
                }
            }
        }

        if (!verified)
        {
            foreach (string wordItem in verbs)
            {
                if (wordsToFind.ToLower() == wordItem.ToLower())
                {
                    verified = true;
                }
            }
        }

        if (!verified)
        {
            foreach (string wordItem in adverbs)
            {
                if (wordsToFind.ToLower() == wordItem.ToLower())
                {
                    verified = true;
                }
            }
        }

        if (!verified)
        {
            foreach (string wordItem in adjectives)
            {
                if (wordsToFind.ToLower() == wordItem.ToLower())
                {
                    verified = true;
                }
            }
        }

        if (!verified)
        {
            foreach (string wordItem in prepositions)
            {
                if (wordsToFind.ToLower() == wordItem.ToLower())
                {
                    verified = true;
                }
            }
        }

        if (!verified)
        {
            foreach (string wordItem in conjuctions)
            {
                if (wordsToFind.ToLower() == wordItem.ToLower())
                {
                    verified = true;
                }
            }
        }  
    }

    /// <summary>
    /// Used to save the brain file as JSON
    /// </summary>
    public void SaveBrainState()
    {
        //Saving wordTypes
        foreach (string key in wordsHashTableHashVal.Keys)
        {
            wordsHashTable += key + ":" + wordsHashTableHashVal[key] + "|";
        }

        wordTypes = new WordTypes(nouns, proNouns, verbs, adverbs, adjectives, prepositions, conjuctions, wordsHashTable, knownWordsDetails, loadedQuestion.questionObjectJSON);

        File.WriteAllText(savingPath, JsonUtility.ToJson(wordTypes));

        Debug.Log("Saved Brain File!");
    }

    /// <summary>
    /// Used to unload JSON of brain file to local variables
    /// </summary>
    public void UnloadJSONToLocal()
    {
        nouns = wordTypes.nouns;
        prepositions = wordTypes.prepositions;
        proNouns = wordTypes.proNouns;
        verbs = wordTypes.verbs;
        adjectives = wordTypes.adjectives;
        adverbs = wordTypes.adverbs;
        conjuctions = wordTypes.conjuctions;

        wordTypesDictionary.Add("nouns", new List<string>());
        wordTypesDictionary.Add("prepositions", new List<string>());
        wordTypesDictionary.Add("proNouns", new List<string>());
        wordTypesDictionary.Add("verbs", new List<string>());
        wordTypesDictionary.Add("adjectives", new List<string>());
        wordTypesDictionary.Add("adverbs", new List<string>());
        wordTypesDictionary.Add("conjuctions", new List<string>());

        foreach (string item in nouns)
        {
            try
            {
                wordTypesDictionary["nouns"].Add(item);
            }
            catch (Exception)
            {
                List<string> tList = new List<string>();
                tList.Add(item);
                wordTypesDictionary.Add("nouns", tList);
            }
        }

        foreach (string item in prepositions)
        {
            try
            {
                wordTypesDictionary["prepositions"].Add(item);
            }
            catch (Exception)
            {
                List<string> tList = new List<string>();
                tList.Add(item);
                wordTypesDictionary.Add("prepositions", tList);
            }
        }

        foreach (string item in proNouns)
        {
            try
            {
                wordTypesDictionary["pronouns"].Add(item);
            }
            catch (Exception)
            {
                List<string> tList = new List<string>();
                tList.Add(item);
                wordTypesDictionary.Add("pronouns", tList);
            }
        }

        foreach (string item in verbs)
        {
            try
            {
                wordTypesDictionary["verbs"].Add(item);
            }
            catch (Exception)
            {
                List<string> tList = new List<string>();
                tList.Add(item);
                wordTypesDictionary.Add("verbs", tList);
            }
        }

        foreach (string item in adjectives)
        {
            try
            {
                wordTypesDictionary["adjectives"].Add(item);
            }
            catch (Exception)
            {
                List<string> tList = new List<string>();
                tList.Add(item);
                wordTypesDictionary.Add("adjectives", tList);
            }
        }

        foreach (string item in adverbs)
        {
            try
            {
                wordTypesDictionary["adverbs"].Add(item);
            }
            catch (Exception)
            {
                List<string> tList = new List<string>();
                tList.Add(item);
                wordTypesDictionary.Add("adverbs", tList);
            }
        }

        foreach (string item in conjuctions)
        {
            try
            {
                wordTypesDictionary["conjuctions"].Add(item);
            }
            catch (Exception)
            {
                List<string> tList = new List<string>();
                tList.Add(item);
                wordTypesDictionary.Add("conjuctions", tList);
            }
        }

        knownWordsDetails.fullWordsDetails = wordTypes.fullWordsDetails;
        wordTypes.knownWordsDetails = knownWordsDetails;
        wordsHashTable = wordTypes.words;
        if (wordTypes.questionSyntax != "" && wordTypes.questionSyntax != " " && wordTypes.questionSyntax != null)
        {
            loadedQuestion.LoadQuestionSyntax(wordTypes.questionSyntax);
        }
      
        Debug.Log("Loaded brain successfully!");

        updateKnownWords();
    }

    /// <summary>
    /// Debugging only
    /// </summary>
    /// <param name="wordTypeEnum"></param>
    /// <param name="word"></param>
    public void debugWordType(WordTypesEnum wordTypeEnum, string word)
    {
        if (wordTypeEnum == WordTypesEnum.noun)
        {
            Debug.Log(word + " is a noun");
        }
        else if (wordTypeEnum == WordTypesEnum.pronoun)
        {
            Debug.Log(word + " is a pronoun");
        }
        else if (wordTypeEnum == WordTypesEnum.preposition)
        {
            Debug.Log(word + " is a preposition");
        }
        else if (wordTypeEnum == WordTypesEnum.verb)
        {
            Debug.Log(word + " is a verb");
        }
        else if (wordTypeEnum == WordTypesEnum.adjective)
        {
            Debug.Log(word + " is an adjective");
        }
        else if (wordTypeEnum == WordTypesEnum.adverb)
        {
            Debug.Log(word + " is an adverb");
        }
        else if (wordTypeEnum == WordTypesEnum.conjuction)
        {
            Debug.Log(word + " is a conjunction");
        }
    }

    /// <summary>
    /// Used to save the brain file with the path provided from the input field.
    /// <para>SeeAlso <seealso cref="saveBrainFileToPath(string)"/> to save the brain file from the command line interface.</para>
    /// </summary>
    public void saveBrainFromInputField()
    {
        loadedQuestion.MixOldAndNewQuestionSyntax(loadedQuestion.qsObj, question.qsObj);

        string path = SavingPath.text + "/TrainedBrain" + Sufix.text + ".brain";

        if (!Directory.Exists(SavingPath.text))
        {
            Directory.CreateDirectory(SavingPath.text);
        }

        if (path != "")
        {
            try
            {
                foreach (string key in knownWordsDetails.fullWordsDetails)
                {
                    wordsHashTable += key + ";";
                }
            }
            catch (Exception)
            {
            }

            //Getting values from lists

            tempWordTypes.nouns = NounsText.text.Split(","[0]);
            tempWordTypes.proNouns = PronounsText.text.Split(","[0]);
            tempWordTypes.verbs = VerbsText.text.Split(","[0]);
            tempWordTypes.adverbs = AdverbsText.text.Split(","[0]);
            tempWordTypes.adjectives = AdjectivesText.text.Split(","[0]);
            tempWordTypes.prepositions = PrepositionsText.text.Split(","[0]);
            tempWordTypes.conjuctions = ConjunctionsText.text.Split(","[0]);
            tempWordTypes.words = wordsHashTable;
            tempWordTypes.knownWordsDetails = knownWordsDetails;
            tempWordTypes.fullWordsDetails = knownWordsDetails.fullWordsDetails;
            question.FinishQuestionTraining();
            tempWordTypes.questionSyntax = loadedQuestion.questionObjectJSON;

            //Initialise brain array
            //nouns
            string[] tempNoun = nouns;
            nouns = new string[nouns.Length + tempWordTypes.nouns.Length];

            int i = 0;
            int j = 0;
            bool unique = true;
            for (i = 0; i < tempNoun.Length; i++)
            {
                if (tempNoun[i] != "" && tempNoun[i] != " ")
                {
                    nouns[i] = tempNoun[i];
                }
            }

            j = 0;
            for (i = tempNoun.Length; i < nouns.Length; i++)
            {
                if (tempWordTypes.nouns[j] != "" && tempWordTypes.nouns[j] != " ")
                {
                    foreach (string item in tempNoun)
                    {
                        if (item == tempWordTypes.nouns[j])
                        {
                            unique = false;
                        }
                    }

                    if (unique)
                    {
                        nouns[i] = tempWordTypes.nouns[j];
                    }
                }
                j++;
            }

            //verbs
            string[] tempverb = verbs;
            verbs = new string[verbs.Length + tempWordTypes.verbs.Length];

            i = 0;
            j = 0;
            for (i = 0; i < tempverb.Length; i++)
            {
                verbs[i] = tempverb[i];
            }

            unique = true;
            for (i = tempverb.Length; i < verbs.Length; i++)
            {
                if (tempWordTypes.verbs[j] != "" && tempWordTypes.verbs[j] != " ")
                {
                    foreach (string item in tempverb)
                    {
                        if (item == tempWordTypes.verbs[j])
                        {
                            unique = false;
                        }
                    }

                    if (unique)
                    {
                        verbs[i] = tempWordTypes.verbs[j];
                    }
                }
                j++;
            }

            //adverbs
            string[] tempAdverb = adverbs;
            adverbs = new string[adverbs.Length + tempWordTypes.adverbs.Length];

            i = 0;
            for (i = 0; i < tempAdverb.Length; i++)
            {
                adverbs[i] = tempAdverb[i];
            }
            j = 0;
            unique = true;
            for (i = tempAdverb.Length; i < adverbs.Length; i++)
            {
                if (tempWordTypes.adverbs[j] != "" && tempWordTypes.adverbs[j] != " ")
                {
                    foreach (string item in tempAdverb)
                    {
                        if (item == tempWordTypes.adverbs[j])
                        {
                            unique = false;
                        }
                    }

                    if (unique)
                    {
                        adverbs[i] = tempWordTypes.adverbs[j];
                    }
                }
            }

            //adjectives
            string[] tempAdj = adjectives;
            adjectives = new string[adjectives.Length + tempWordTypes.adjectives.Length];

            i = 0;
            for (i = 0; i < tempAdj.Length; i++)
            {
                adjectives[i] = tempAdj[i];
            }
            j = 0;
            unique = true;
            for (i = tempAdj.Length; i < adjectives.Length; i++)
            {
                if (tempWordTypes.adjectives[j] != "" && tempWordTypes.adjectives[j] != " ")
                {
                    foreach (string item in tempAdj)
                    {
                        if (item == tempWordTypes.adjectives[j])
                        {
                            unique = false;
                        }
                    }

                    if (unique)
                    {
                        adjectives[i] = tempWordTypes.adjectives[j];
                    }
                }
            }

            //prepositions
            string[] tempPrep = prepositions;
            prepositions = new string[prepositions.Length + tempWordTypes.prepositions.Length];

            i = 0;
            for (i = 0; i < tempPrep.Length; i++)
            {
                prepositions[i] = tempPrep[i];
            }
            j = 0;
            unique = true;
            for (i = tempPrep.Length; i < prepositions.Length; i++)
            {
                if (tempWordTypes.prepositions[j] != "" && tempWordTypes.prepositions[j] != " ")
                {
                    foreach (string item in tempPrep)
                    {
                        if (item == tempWordTypes.prepositions[j])
                        {
                            unique = false;
                        }
                    }

                    if (unique)
                    {
                        prepositions[i] = tempWordTypes.prepositions[j];
                    }
                }
            }

            //pronouns
            string[] tempproNound = proNouns;
            proNouns = new string[proNouns.Length + tempWordTypes.proNouns.Length];

            i = 0;
            for (i = 0; i < tempproNound.Length; i++)
            {
                proNouns[i] = tempproNound[i];
            }
            j = 0;
            unique = true;
            for (i = tempproNound.Length; i < proNouns.Length; i++)
            {
                if (tempWordTypes.proNouns[j] != "" && tempWordTypes.proNouns[j] != " ")
                {
                    foreach (string item in tempproNound)
                    {
                        if (item == tempWordTypes.proNouns[j])
                        {
                            unique = false;
                        }
                    }

                    if (unique)
                    {
                        proNouns[i] = tempWordTypes.proNouns[j];
                    }
                }
            }

            //conjunctions
            string[] tempConjuction = conjuctions;
            conjuctions = new string[conjuctions.Length + tempWordTypes.conjuctions.Length];

            i = 0;
            for (i = 0; i < tempConjuction.Length; i++)
            {
                conjuctions[i] = tempConjuction[i];
            }
            j = 0;
            unique = true;
            for (i = tempConjuction.Length; i < conjuctions.Length; i++)
            {
                if (tempWordTypes.conjuctions[j] != "" && tempWordTypes.conjuctions[j] != " ")
                {
                    foreach (string item in tempConjuction)
                    {
                        if (item == tempWordTypes.conjuctions[j])
                        {
                            unique = false;
                        }
                    }

                    if (unique)
                    {
                        conjuctions[i] = tempWordTypes.conjuctions[j];
                    }
                }
            }

            int blanks = 0;
            int blankPos = 0;
            string[] temp = nouns;

            foreach (string item in temp)
            {
                if (item == "" || item == " ")
                {
                    blanks++;
                }
            }

            if (blanks > 0)
            {
                nouns = new string[temp.Length - blanks];

                blankPos = 0;
                for (int hhj = 0; hhj < temp.Length; hhj++)
                {
                    if (temp[hhj] != "" && temp[hhj] != " ")
                    {
                        nouns[blankPos] = temp[hhj];
                        blankPos++;
                    }
                }
            }

            blanks = 0;

            temp = proNouns;

            foreach (string item in temp)
            {
                if (item == "" || item == " ")
                {
                    blanks++;
                }
            }

            if (blanks > 0)
            {
                proNouns = new string[temp.Length - blanks];

                blankPos = 0;
                for (int hhj = 0; hhj < temp.Length; hhj++)
                {
                    if (temp[hhj] != "" && temp[hhj] != " ")
                    {
                        proNouns[blankPos] = temp[hhj];
                        blankPos++;
                    }
                }
            }

            blanks = 0;

            temp = verbs;

            foreach (string item in temp)
            {
                if (item == "" || item == " ")
                {
                    blanks++;
                }
            }

            if (blanks > 0)
            {
                verbs = new string[temp.Length - blanks];

                blankPos = 0;
                for (int hhj = 0; hhj < temp.Length; hhj++)
                {
                    if (temp[hhj] != "" && temp[hhj] != " ")
                    {
                        verbs[blankPos] = temp[hhj];
                        blankPos++;
                    }
                }
            }

            blanks = 0;

            temp = adverbs;

            foreach (string item in temp)
            {
                if (item == "" || item == " ")
                {
                    blanks++;
                }
            }

            if (blanks > 0)
            {
                adverbs = new string[temp.Length - blanks];

                blankPos = 0;
                for (int hhj = 0; hhj < temp.Length; hhj++)
                {
                    if (temp[hhj] != "" && temp[hhj] != " ")
                    {
                        adverbs[blankPos] = temp[hhj];
                        blankPos++;
                    }
                }
            }

            blanks = 0;

            temp = adjectives;

            foreach (string item in temp)
            {
                if (item == "" || item == " ")
                {
                    blanks++;
                }
            }

            if (blanks > 0)
            {
                adjectives = new string[temp.Length - blanks];

                blankPos = 0;
                for (int hhj = 0; hhj < temp.Length; hhj++)
                {
                    if (temp[hhj] != "" && temp[hhj] != " ")
                    {
                        adjectives[blankPos] = temp[hhj];
                        blankPos++;
                    }
                }
            }

            blanks = 0;

            temp = conjuctions;

            foreach (string item in temp)
            {
                if (item == "" || item == " ")
                {
                    blanks++;
                }
            }

            if (blanks > 0)
            {
                conjuctions = new string[temp.Length - blanks];

                blankPos = 0;
                for (int hhj = 0; hhj < temp.Length; hhj++)
                {
                    if (temp[hhj] != "" && temp[hhj] != " ")
                    {
                        conjuctions[blankPos] = temp[hhj];
                        blankPos++;
                    }
                }
            }

            blanks = 0;

            temp = prepositions;

            foreach (string item in temp)
            {
                if (item == "" || item == " ")
                {
                    blanks++;
                }
            }

            if (blanks > 0)
            {
                prepositions = new string[temp.Length - blanks];

                blankPos = 0;
                for (int hhj = 0; hhj < temp.Length; hhj++)
                {
                    if (temp[hhj] != "" && temp[hhj] != " ")
                    {
                        prepositions[blankPos] = temp[hhj];
                        blankPos++;
                    }
                }
            }

            blanks = 0;

            wordTypes = new WordTypes(nouns, proNouns, verbs, adverbs, adjectives, prepositions, conjuctions, wordsHashTable, knownWordsDetails, loadedQuestion.questionObjectJSON);

            File.WriteAllText(path, JsonUtility.ToJson(wordTypes));

            Debug.Log("Saved Brain File!" + path);
        }
    }

    /// <summary>
    /// This method is for saving the brain file from cmd to a file path.
    /// </summary>
    /// <param name="filePath">The file path to store the brain file to.</param>
    public void saveBrainFileToPath(string filePath)
    {

        string path = filePath + ".brain";
        Debug.Log("Path = " + path);

        if (!path.Contains("/"))
        {
            path = path.Replace("\\", "/");
            Debug.Log("Path = " + path);
        }

        if (!Directory.Exists(path.Substring(0, path.LastIndexOf("/"))))
        {
            Directory.CreateDirectory(path.Substring(0, path.LastIndexOf("/")));
        }

        if (path != "")
        {
            try
            {
                foreach (string key in knownWordsDetails.fullWordsDetails)
                {
                    wordsHashTable += key + ";";
                }
            }
            catch (Exception)
            {
            }

            //Getting values from lists
            tempWordTypes.nouns = NounsText.text.Split(","[0]);
            tempWordTypes.proNouns = PronounsText.text.Split(","[0]);
            tempWordTypes.verbs = VerbsText.text.Split(","[0]);
            tempWordTypes.adverbs = AdverbsText.text.Split(","[0]);
            tempWordTypes.adjectives = AdjectivesText.text.Split(","[0]);
            tempWordTypes.prepositions = PrepositionsText.text.Split(","[0]);
            tempWordTypes.conjuctions = ConjunctionsText.text.Split(","[0]);
            tempWordTypes.knownWordsDetails = knownWordsDetails;
            tempWordTypes.words = wordsHashTable;
            tempWordTypes.fullWordsDetails = knownWordsDetails.fullWordsDetails;

            question.FinishQuestionTraining();
            loadedQuestion.MixOldAndNewQuestionSyntax(loadedQuestion.qsObj, question.qsObj);

            tempWordTypes.questionSyntax = loadedQuestion.questionObjectJSON;

            //Initialise brain array
            //nouns
            string[] tempNoun = nouns;
            nouns = new string[nouns.Length + tempWordTypes.nouns.Length];

            int i = 0;
            int j = 0;
            bool unique = true;
            for (i = 0; i < tempNoun.Length; i++)
            {
                if (tempNoun[i] != "" && tempNoun[i] != " ")
                {
                    nouns[i] = tempNoun[i];
                }
            }

            j = 0;
            for (i = tempNoun.Length; i < nouns.Length; i++)
            {
                if (tempWordTypes.nouns[j] != "" && tempWordTypes.nouns[j] != " ")
                {
                    foreach (string item in tempNoun)
                    {
                        if (item == tempWordTypes.nouns[j])
                        {
                            unique = false;
                        }
                    }

                    if (unique)
                    {
                        nouns[i] = tempWordTypes.nouns[j];
                    }
                }
                j++;
            }

            //verbs
            string[] tempverb = verbs;
            verbs = new string[verbs.Length + tempWordTypes.verbs.Length];

            i = 0;
            j = 0;
            for (i = 0; i < tempverb.Length; i++)
            {
                verbs[i] = tempverb[i];
            }

            unique = true;
            for (i = tempverb.Length; i < verbs.Length; i++)
            {
                if (tempWordTypes.verbs[j] != "" && tempWordTypes.verbs[j] != " ")
                {
                    foreach (string item in tempverb)
                    {
                        if (item == tempWordTypes.verbs[j])
                        {
                            unique = false;
                        }
                    }

                    if (unique)
                    {
                        verbs[i] = tempWordTypes.verbs[j];
                    }
                }
                j++;
            }

            //adverbs
            string[] tempAdverb = adverbs;
            adverbs = new string[adverbs.Length + tempWordTypes.adverbs.Length];

            i = 0;
            for (i = 0; i < tempAdverb.Length; i++)
            {
                adverbs[i] = tempAdverb[i];
            }
            j = 0;
            unique = true;
            for (i = tempAdverb.Length; i < adverbs.Length; i++)
            {
                if (tempWordTypes.adverbs[j] != "" && tempWordTypes.adverbs[j] != " ")
                {
                    foreach (string item in tempAdverb)
                    {
                        if (item == tempWordTypes.adverbs[j])
                        {
                            unique = false;
                        }
                    }

                    if (unique)
                    {
                        adverbs[i] = tempWordTypes.adverbs[j];
                    }
                }
            }

            //adjectives
            string[] tempAdj = adjectives;
            adjectives = new string[adjectives.Length + tempWordTypes.adjectives.Length];

            i = 0;
            for (i = 0; i < tempAdj.Length; i++)
            {
                adjectives[i] = tempAdj[i];
            }
            j = 0;
            unique = true;
            for (i = tempAdj.Length; i < adjectives.Length; i++)
            {
                if (tempWordTypes.adjectives[j] != "" && tempWordTypes.adjectives[j] != " ")
                {
                    foreach (string item in tempAdj)
                    {
                        if (item == tempWordTypes.adjectives[j])
                        {
                            unique = false;
                        }
                    }

                    if (unique)
                    {
                        adjectives[i] = tempWordTypes.adjectives[j];
                    }
                }
            }

            //prepositions
            string[] tempPrep = prepositions;
            prepositions = new string[prepositions.Length + tempWordTypes.prepositions.Length];

            i = 0;
            for (i = 0; i < tempPrep.Length; i++)
            {
                prepositions[i] = tempPrep[i];
            }
            j = 0;
            unique = true;
            for (i = tempPrep.Length; i < prepositions.Length; i++)
            {
                if (tempWordTypes.prepositions[j] != "" && tempWordTypes.prepositions[j] != " ")
                {
                    foreach (string item in tempPrep)
                    {
                        if (item == tempWordTypes.prepositions[j])
                        {
                            unique = false;
                        }
                    }

                    if (unique)
                    {
                        prepositions[i] = tempWordTypes.prepositions[j];
                    }
                }
            }

            //pronouns
            string[] tempproNound = proNouns;
            proNouns = new string[proNouns.Length + tempWordTypes.proNouns.Length];

            i = 0;
            for (i = 0; i < tempproNound.Length; i++)
            {
                proNouns[i] = tempproNound[i];
            }
            j = 0;
            unique = true;
            for (i = tempproNound.Length; i < proNouns.Length; i++)
            {
                if (tempWordTypes.proNouns[j] != "" && tempWordTypes.proNouns[j] != " ")
                {
                    foreach (string item in tempproNound)
                    {
                        if (item == tempWordTypes.proNouns[j])
                        {
                            unique = false;
                        }
                    }

                    if (unique)
                    {
                        proNouns[i] = tempWordTypes.proNouns[j];
                    }
                }
            }

            //conjunctions
            string[] tempConjuction = conjuctions;
            conjuctions = new string[conjuctions.Length + tempWordTypes.conjuctions.Length];

            i = 0;
            for (i = 0; i < tempConjuction.Length; i++)
            {
                conjuctions[i] = tempConjuction[i];
            }
            j = 0;
            unique = true;
            for (i = tempConjuction.Length; i < conjuctions.Length; i++)
            {
                if (tempWordTypes.conjuctions[j] != "" && tempWordTypes.conjuctions[j] != " ")
                {
                    foreach (string item in tempConjuction)
                    {
                        if (item == tempWordTypes.conjuctions[j])
                        {
                            unique = false;
                        }
                    }

                    if (unique)
                    {
                        conjuctions[i] = tempWordTypes.conjuctions[j];
                    }
                }
            }

            int blanks = 0;
            int blankPos = 0;
            string[] temp = nouns;

            foreach (string item in temp)
            {
                if (item == "" || item == " ")
                {
                    blanks++;
                }
            }

            if (blanks > 0)
            {
                nouns = new string[temp.Length - blanks];

                blankPos = 0;
                for (int hhj = 0; hhj < temp.Length; hhj ++)
                {
                    if (temp[hhj] != "" && temp[hhj] != " ")
                    {
                        nouns[blankPos] = temp[hhj];
                        blankPos++;
                    }
                }
            }

            blanks = 0;

            temp = proNouns;

            foreach (string item in temp)
            {
                if (item == "" || item == " ")
                {
                    blanks++;
                }
            }

            if (blanks > 0)
            {
                proNouns = new string[temp.Length - blanks];

                blankPos = 0;
                for (int hhj = 0; hhj < temp.Length; hhj++)
                {
                    if (temp[hhj] != "" && temp[hhj] != " ")
                    {
                        proNouns[blankPos] = temp[hhj];
                        blankPos++;
                    }
                }
            }

            blanks = 0;

            temp = verbs;

            foreach (string item in temp)
            {
                if (item == "" || item == " ")
                {
                    blanks++;
                }
            }

            if (blanks > 0)
            {
                verbs = new string[temp.Length - blanks];

                blankPos = 0;
                for (int hhj = 0; hhj < temp.Length; hhj++)
                {
                    if (temp[hhj] != "" && temp[hhj] != " ")
                    {
                        verbs[blankPos] = temp[hhj];
                        blankPos++;
                    }
                }
            }

            blanks = 0;

            temp = adverbs;

            foreach (string item in temp)
            {
                if (item == "" || item == " ")
                {
                    blanks++;
                }
            }

            if (blanks > 0)
            {
                adverbs = new string[temp.Length - blanks];

                blankPos = 0;
                for (int hhj = 0; hhj < temp.Length; hhj++)
                {
                    if (temp[hhj] != "" && temp[hhj] != " ")
                    {
                        adverbs[blankPos] = temp[hhj];
                        blankPos++;
                    }
                }
            }

            blanks = 0;

            temp = adjectives;

            foreach (string item in temp)
            {
                if (item == "" || item == " ")
                {
                    blanks++;
                }
            }

            if (blanks > 0)
            {
                adjectives = new string[temp.Length - blanks];

                blankPos = 0;
                for (int hhj = 0; hhj < temp.Length; hhj++)
                {
                    if (temp[hhj] != "" && temp[hhj] != " ")
                    {
                        adjectives[blankPos] = temp[hhj];
                        blankPos++;
                    }
                }
            }

            blanks = 0;

            temp = conjuctions;

            foreach (string item in temp)
            {
                if (item == "" || item == " ")
                {
                    blanks++;
                }
            }

            if (blanks > 0)
            {
                conjuctions = new string[temp.Length - blanks];

                blankPos = 0;
                for (int hhj = 0; hhj < temp.Length; hhj++)
                {
                    if (temp[hhj] != "" && temp[hhj] != " ")
                    {
                        conjuctions[blankPos] = temp[hhj];
                        blankPos++;
                    }
                }
            }

            blanks = 0;

            temp = prepositions;

            foreach (string item in temp)
            {
                if (item == "" || item == " ")
                {
                    blanks++;
                }
            }

            if (blanks > 0)
            {
                prepositions = new string[temp.Length - blanks];

                blankPos = 0;
                for (int hhj = 0; hhj < temp.Length; hhj++)
                {
                    if (temp[hhj] != "" && temp[hhj] != " ")
                    {
                        prepositions[blankPos] = temp[hhj];
                        blankPos++;
                    }
                }
            }

            blanks = 0;

            wordTypes = new WordTypes(nouns, proNouns, verbs, adverbs, adjectives, prepositions, conjuctions,wordsHashTable, knownWordsDetails, loadedQuestion.questionObjectJSON);

            File.WriteAllText(path, JsonUtility.ToJson(wordTypes));

            Debug.Log("Saved Brain File!" + path);
        }
    }

    /// <summary>
    /// Used to load the brain file
    /// </summary>
    public void loadBrainFile()
    {
        if (File.Exists(loadingPath.text))
        {
            if (loadingPath.text.Contains(".brain"))
            {
                //Read from existing brain file
                wordTypes = JsonUtility.FromJson<WordTypes>(File.ReadAllText(loadingPath.text));

                UnloadJSONToLocal();

                PlayerPrefs.SetInt("useLoadedBrain", 0);
                PlayerPrefs.SetString("brainPath", loadingPath.text);

                loadBrainNotice.SetActive(false);
            }
            else
            {
                messageNotice.text = "Incorrect File Type! Make sure it is a .brain file!";
            }
        }
        else
        {
            messageNotice.text = "File does not exists!";
        }
    }

    /// <summary>
    /// Use this method to update old brain files to newer versions
    /// <para><paramref name="brainJSON"/> : The JSON for the old brain file</para>
    /// </summary>
    /// <param name="brainJSON">The JSON for the old brain file</param>
    /// <remarks>Deprecated</remarks>
    public void UpdateOldBrain(string brainJSON)
    {
        WordTypes oldBrain = JsonUtility.FromJson<WordTypes>(brainJSON);

        try
        {
            string temp = oldBrain.fullWordsDetails[0];
        }
        catch (Exception)
        {
            oldBrain.fullWordsDetails = new string[0];
        }

        try
        {
            string temp = oldBrain.questionSyntax;

            if (oldBrain.questionSyntax == "")
            {

            }
        }
        catch (Exception)
        {
            oldBrain.fullWordsDetails = new string[0];
        }

        try
        {
            string temp = oldBrain.fullWordsDetails[0];
        }
        catch (Exception)
        {
            oldBrain.fullWordsDetails = new string[0];
        }

        try
        {
            string temp = oldBrain.fullWordsDetails[0];
        }
        catch (Exception)
        {
            oldBrain.fullWordsDetails = new string[0];
        }
    }

    /// <summary>
    /// Contains reference to all supported Word Types
    /// </summary>
    public enum WordTypesEnum 
    {
        noun,
        pronoun,
        verb,
        adverb,
        adjective,
        preposition,
        conjuction,
        nothing
    }

    /// <summary>
    /// Contains reference to all supported Question Descriptors
    /// </summary>
    public enum Questions
    {
        What,
        Why,
        When,
        Where,
        Who,
        How
    }
}
