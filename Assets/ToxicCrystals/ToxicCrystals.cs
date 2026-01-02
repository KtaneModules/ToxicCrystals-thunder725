using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class ToxicCrystals : MonoBehaviour {

    // Visual & Audio Data
    [Header("Visual Data")]
    Material backgroundCasingMaterial;
    [SerializeField] MeshRenderer backgroundCasingMeshRenderer;
    float currentBackgroundAnimationTimer;
    [SerializeField] float scaledTimeBetweenBackgroundAnimations;
    [SerializeField] float backgroundAnimationSpeed;
    [SerializeField] TextMesh casingLetterTextMesh;
    Vector3[] startingCrystalPositions, startingCrystalRotations;
    float crystalsMovementTimer;
    [SerializeField] float crystalsWiggleMovementSpeed;
    [SerializeField] float crystalsWiggleMovementIntensity, crystalsWiggleRotationIntensity;
    [SerializeField] Material unlitCrystalMaterial, concentratedCrysalMaterial;
    [SerializeField] MeshRenderer[] crystalsMeshRenderers;
    [SerializeField] float concentratedCrystalMaterialHueSpeed;
    float concentratedCrystalMaterialHueTimer;

    [SerializeField] AudioClip successfulPressSound, moduleSolvedSound;


    // Bomb Data
    [Header("Bomb Data")]
    [SerializeField] KMBombModule thisModule;
    [SerializeField] KMBombInfo bombInfo;
    [SerializeField] KMAudio audioSubmodule;


    // Puzzle Data
    [Header("Puzzle Data")]
    [SerializeField] KMSelectable[] pressableCrystals;
    int[] concentratedCrystalsPositions;
    string currentCasingLetter;
    readonly int[] CrystalsKeysTable = new int[70]
    { 6, 5, 7, 4, 2, 2, 4,
      5, 7, 1, 5, 1, 2, 3,
      2, 7, 5, 5, 3, 1, 3,
      7, 4, 6, 2, 2, 7, 1,
      1, 2, 6, 5, 6, 7, 4,
      4, 5, 7, 4, 1, 3, 7,
      5, 2, 7, 1, 4, 3, 6,
      1, 3, 5, 2, 3, 7, 4,
      2, 4, 6, 1, 3, 6, 6,
      3, 6, 4, 6, 5, 1, 3 };
    readonly Dictionary<string, int[]> CasingLetterTable = new Dictionary<string, int[]>
    {
        {"A", new int[]{ 8, 4, 3} }, {"B", new int[]{ 1, 3, 8} }, {"C", new int[]{ 1, 5, 0} },
        {"D", new int[]{ 7, 4, 2} }, {"E", new int[]{ 4, 6, 3} }, {"F", new int[]{ 2, 0, 1} },
        {"G", new int[]{ 3, 8, 2} }, {"H", new int[]{ 2, 3, 6} }, {"I", new int[]{ 7, 8, 6} },
        {"J", new int[]{ 6, 5, 8} }, {"K", new int[]{ 1, 7, 5} }, {"L", new int[]{ 5, 1, 8} },
        {"M", new int[]{ 5, 3, 7} }, {"N", new int[]{ 0, 2, 6} }, {"O", new int[]{ 1, 7, 4} },
        {"P", new int[]{ 2, 1, 4} }, {"Q", new int[]{ 3, 8, 1} }, {"R", new int[]{ 6, 7, 4} },
        {"S", new int[]{ 7, 4, 6} }, {"T", new int[]{ 0, 6, 1} }, {"U", new int[]{ 5, 2, 6} },
        {"V", new int[]{ 5, 4, 8} }, {"W", new int[]{ 7, 0, 3} }, {"X", new int[]{ 0, 3, 5} },
        {"Y", new int[]{ 4, 0, 7} }, {"Z", new int[]{ 0, 8, 2} }, {"?", new int[]{ 5, 0, 2} },

    };
    int[] generatedKeys;
    int currentNumberOfSolves, previousNumberOfSolves;
    Coroutine checkForSolvedCoroutine;
    List<string> allRegisteredSolvedModules;
    List<int> pressedCrystalsIndices;
    int[] crystalsToPress;
    int currentCrystalToPressIndex;


    // Logging Data
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;


    // Souvenir & Twitch Plays Data




    // Buttons gathering and GetComponents
    void Awake()
    {
        // Initialize Logging
        moduleId = moduleIdCounter++;


        // Create a new material instance, to avoid editing every Toxic Crystals modules at once
        // That would look very cool, but that would require centralizing all codes and stuff so nope
        backgroundCasingMaterial = Instantiate<Material>(backgroundCasingMeshRenderer.material);
        backgroundCasingMeshRenderer.material = backgroundCasingMaterial;

    }



    // Puzzle Initialization
    void Start ()
    {
        Debug.LogFormat("[Toxic Crystals #{0}] Initializing Module.", moduleId);

        startingCrystalPositions = new Vector3[7];
        startingCrystalRotations = new Vector3[7];
        crystalsMovementTimer = UnityEngine.Random.Range(0f, 25f);
        concentratedCrystalMaterialHueTimer = UnityEngine.Random.Range(0f, 1f);

        unlitCrystalMaterial = Instantiate(unlitCrystalMaterial);
        concentratedCrysalMaterial = Instantiate(concentratedCrysalMaterial);

        for (int i = 0; i < 7; i++)
        {
            int _j = i;
            startingCrystalPositions[i] = pressableCrystals[i].transform.localPosition;
            startingCrystalRotations[i] = pressableCrystals[i].transform.localEulerAngles;

            // Re-creating a variable each loop circumvents a problem with initializing delegates in for loops
            // If we just pass "i", it will end up being at 7 and so it will try to send pressableCrystals[7] which obviously doesn't exist.
            pressableCrystals[i].OnInteract += delegate () { CrystalButtonGetsPressed(pressableCrystals[_j], _j); return false; };
        }
        pressedCrystalsIndices = new List<int>();

        InitializeBackgroundAnimation();

        InitializePuzzle();

        allRegisteredSolvedModules = new List<string>();
        checkForSolvedCoroutine = StartCoroutine(CheckForNewlySolvedModules());

    }
	


	void Update ()
    {
        UpdateBackgroundCasingAnimation();

        WiggleCrystalsAround();

        UpdateConcentratedCrystalsHue();
    }




    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Puzzle Functions
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void InitializePuzzle()
    {
        // Initialize Casing Letter
        Debug.LogFormat("[Toxic Crystals #{0}] Starting casing letter is always “T”.", moduleId);
        currentCasingLetter = "T";
        UpdateCasingLetterVisual();


        GenerateKeys();
        Debug.LogFormat("[Toxic Crystals #{0}] Generated keys are: {1}, {2} and {3}.", moduleId, generatedKeys[0], generatedKeys[1], generatedKeys[2]);

        GenerateNewConcentratedCrystals();


        crystalsToPress = new int[3];
        GenerateNewSolution();
    }



    void GenerateKeys()
    {
        Debug.LogFormat("[Toxic Crystals #{0}] Generating Keys...", moduleId);

        // If all digits in the Serial Number are Prime, use 089
        // Credit for this line using string.Join to https://discussions.unity.com/t/array-of-ints-to-string/645204/3
        string _serialNumberNumbersString = string.Join("", bombInfo.GetSerialNumberNumbers().ToArray().Select(i => i.ToString()).ToArray()); 
        Debug.LogFormat("[Toxic Crystals #{0}] All digits in Serial Number are: {1}", moduleId, _serialNumberNumbersString);

        // " ^[2357]+$ " should return true only if ALL characters are included in 2357
        if (Regex.IsMatch(_serialNumberNumbersString, "^[2357]+$"))
        {
            Debug.LogFormat("[Toxic Crystals #{0}] All digits are prime.", moduleId);
            generatedKeys = new int[3] { 0, 8, 9 };
            return;
        }
        Debug.LogFormat("[Toxic Crystals #{0}] Not all digits are prime.", moduleId);




        // Otherwise, if the all Serial Number digits multiplied together is greater than 38, use “520”.
        int[] _serialNumberNumbers = bombInfo.GetSerialNumberNumbers().ToArray<int>();
        switch (_serialNumberNumbers.Length)
        {
            case 0:
                Debug.LogFormat("[Toxic Crystals #{0}] No Serial Number digits found. Total is lesser than 38.", moduleId);
                break;

            // No way a single digit is above 38, just break
            case 1:
                Debug.LogFormat("[Toxic Crystals #{0}] Only one Serial Number digit found. Total is lesser than 38", moduleId);
                break;

            default:
                int _result = _serialNumberNumbers[0];
                for (int i = 1; i < _serialNumberNumbers.Length; i++)
                {
                    _result *= _serialNumberNumbers[i];
                }

                if (_result > 38)
                {
                    Debug.LogFormat("[Toxic Crystals #{0}] Product of all digits is {1}, which is more than 38.", moduleId, _result);
                    generatedKeys = new int[3] { 5, 2, 0 };
                    return;
                }
                Debug.LogFormat("[Toxic Crystals #{0}] Product of all digits is {1}, which is lesser or equal than 38.", moduleId, _result);
                break;
        }


        // Otherwise, if there are more Unlit indicators than Lits, use “216”.
        int _unlitIndicators = bombInfo.GetOffIndicators().Count();
        int _litIndicators = bombInfo.GetOnIndicators().Count();

        if (_unlitIndicators > _litIndicators)
        {
            Debug.LogFormat("[Toxic Crystals #{0}] There are {1} Unlit Indicators and {2} Lit Indicators. {1} is greater than {2}", moduleId, _unlitIndicators, _litIndicators);
            generatedKeys = new int[3] { 2, 1, 6 };
            return;
        }
        Debug.LogFormat("[Toxic Crystals #{0}] There are {1} Unlit Indicators and {2} Lit Indicators. {1} is not greater than {2}", moduleId, _unlitIndicators, _litIndicators);



        // Otherwise, if at least one Parallel port is present, use “347”.
        bool _hasParallelPort = bombInfo.GetPorts().ToArray<string>().Contains("Parallel");
        if (_hasParallelPort)
        {
            Debug.LogFormat("[Toxic Crystals #{0}] Parallel Port detected.", moduleId);
            generatedKeys = new int[3] { 3, 4, 7 };
            return;
        }
        Debug.LogFormat("[Toxic Crystals #{0}] Parallel Port not detected.", moduleId);



        // Otherwise, use “038”.
        Debug.LogFormat("[Toxic Crystals #{0}] All other rules were false.", moduleId);
        generatedKeys = new int[3] { 0, 3, 8 };
    }



    IEnumerator CheckForNewlySolvedModules()
    {
        // Code from DuckKonundrum module
        // Loop, only until we haven't started submitting
        while (pressedCrystalsIndices.Count == 0)
        {
            // Get current number of Solves
            currentNumberOfSolves = bombInfo.GetSolvedModuleNames().Count;
            // Compare with last known stage number
            if (currentNumberOfSolves > previousNumberOfSolves)
            {
                NewModuleGotSolved();
            }

            yield return new WaitForSeconds(.1f);
        }
    }



    void NewModuleGotSolved()
    {
        // This function is only called if there is for sure at least one new module solved!
        previousNumberOfSolves++;


        // Since this is filtered, we can do heavier code.
        // Code credit to VFlyer & Blananas2 from Übermodule


        // Get all currently solved Modules' names
        var _allSolvedModules = bombInfo.GetSolvedModuleNames();

        // Remove every known one
        foreach (string _solved in allRegisteredSolvedModules)
        {
            _allSolvedModules.Remove(_solved);
        }

        // What we're left with is a List of all modules that have been solved since last time we checked
        // Hope we have at least one... Otherwise something bad happened!
        // Catch the exception though
        if (_allSolvedModules.Count < 1)
        {
            Debug.LogFormat("[Toxic Crystals #{0}] WOOPS! Something very bad happened!! Contact thunder725 with a log please! (Error: NewModuleFalsePositive)", moduleId);
            RegisterNewCasingLetterFromSolvedModule("?");
            return;
        }

        // Register the solved module!
        allRegisteredSolvedModules.Add(_allSolvedModules[0]);
        Debug.LogFormat("[Toxic Crystals #{0}] =-=-= New module got solved: {1} =-=-=", moduleId, _allSolvedModules[0]);
        RegisterNewCasingLetterFromSolvedModule(_allSolvedModules[0]);

        GenerateNewConcentratedCrystals();

        GenerateNewSolution();
    }



    void RegisterNewCasingLetterFromSolvedModule(string moduleName)
    {
        // Catch the exception of an empty module name.
        if (moduleName.Length == 0)
        {
            Debug.LogFormat("[Toxic Crystals #{0}] Got empty module name!!! Using '?' for Casing Letter as safety measure.", moduleId);
            currentCasingLetter = "?";
            UpdateCasingLetterVisual();
            return;
        }

        // Remove all Spaces in the name
        string _workingModuleName = moduleName.Replace(" ", "").ToUpperInvariant();

        // Remove leading "THE" if it exists
        if (_workingModuleName.Substring(0, 3) == "THE")
        {
            _workingModuleName = _workingModuleName.Substring(3);
        }

        // Module name with just "THE"? Handle that too!
        if (_workingModuleName.Length == 0)
        {
            Debug.LogFormat("[Toxic Crystals #{0}] That module is just called ''THE''! Using '?' for Casing Letter as safety measure.", moduleId);
            currentCasingLetter = "?";
            UpdateCasingLetterVisual();
            return;
        }

        // If the first letter is not a Letter (A-Z)
        if (!Regex.IsMatch(_workingModuleName[0].ToString(), "[A-Z]"))
        {
            Debug.LogFormat("[Toxic Crystals #{0}] Received letter '{1}' is not an English Alphabet Letter. Using '?' for Casing Letter.", moduleId, _workingModuleName[0]);
            currentCasingLetter = "?";
            UpdateCasingLetterVisual();
            return;
        }

        Debug.LogFormat("[Toxic Crystals #{0}] Received letter '{1}' is an English Alphabet Letter. Keeping '{1}' for Casing Letter.", moduleId, _workingModuleName[0]);
        currentCasingLetter = _workingModuleName[0].ToString();
        UpdateCasingLetterVisual();

    }


    void GenerateNewConcentratedCrystals()
    {
        // Get three distinct values in the range 1-7.
        concentratedCrystalsPositions = (new int[] { 1, 2, 3, 4, 5, 6, 7 }).Shuffle().Take(3).ToArray();

        Debug.LogFormat("[Toxic Crystals #{0}] Newly generated Concentrated Crystals' positions: {1}, {2}, {3}.", moduleId, concentratedCrystalsPositions[0], concentratedCrystalsPositions[1], concentratedCrystalsPositions[2]);

        // Set the correct Material
        for (int i = 0; i < 7; i ++)
        {
            crystalsMeshRenderers[i].material = concentratedCrystalsPositions.Contains(i + 1) ? concentratedCrysalMaterial : unlitCrystalMaterial;
        }
    }


    void GenerateNewSolution()
    {
        // All intersections's Indices in reading order, from TopLeft to BottomRight
        int[] _intersectionIndices = new int[9];


        // It's gonna be way easier if Keys and Crystals are ordered.
        int[] _sortedCrystals = concentratedCrystalsPositions.OrderBy(x => x).ToArray();
        int[] _sortedKeys = generatedKeys.OrderBy(x => x).ToArray();


        // In CrystalsKeysTable:
        // CellIndex = (Crystal - 1) + (7 * Key)
        // Don't forget that Crystals are 1-Indexed
        _intersectionIndices[0] = _sortedKeys[0] * 7 + _sortedCrystals[0] - 1;
        _intersectionIndices[1] = _sortedKeys[0] * 7 + _sortedCrystals[1] - 1;
        _intersectionIndices[2] = _sortedKeys[0] * 7 + _sortedCrystals[2] - 1;
        _intersectionIndices[3] = _sortedKeys[1] * 7 + _sortedCrystals[0] - 1;
        _intersectionIndices[4] = _sortedKeys[1] * 7 + _sortedCrystals[1] - 1;
        _intersectionIndices[5] = _sortedKeys[1] * 7 + _sortedCrystals[2] - 1;
        _intersectionIndices[6] = _sortedKeys[2] * 7 + _sortedCrystals[0] - 1;
        _intersectionIndices[7] = _sortedKeys[2] * 7 + _sortedCrystals[1] - 1;
        _intersectionIndices[8] = _sortedKeys[2] * 7 + _sortedCrystals[2] - 1;


        Debug.LogFormat("[Toxic Crystals #{0}] New Intersection values in reading order are: {1} {2} {3}  /  {4} {5} {6}  /  {7} {8} {9}", moduleId,
            CrystalsKeysTable[_intersectionIndices[0]], CrystalsKeysTable[_intersectionIndices[1]], CrystalsKeysTable[_intersectionIndices[2]],
            CrystalsKeysTable[_intersectionIndices[3]], CrystalsKeysTable[_intersectionIndices[4]], CrystalsKeysTable[_intersectionIndices[5]],
            CrystalsKeysTable[_intersectionIndices[6]], CrystalsKeysTable[_intersectionIndices[7]], CrystalsKeysTable[_intersectionIndices[8]]);



        // Get the intersections to keep from the Casing Letter Table
        int[] _intersectionsToKeep;
        if (!CasingLetterTable.TryGetValue(currentCasingLetter, out _intersectionsToKeep))
        {
            Debug.LogFormat("[Toxic Crystals #{0}] WOOPS! Something very bad happened!! Contact thunder725 with a log please! (Error: UnknownCaracterCasingTable)", moduleId);
            crystalsToPress = new int[3] { 0, 1, 2 };
            return;
        }

        Debug.LogFormat("[Toxic Crystals #{0}] New Intersections to keep are {1}, {2} and {3} in this order.", moduleId,
            GetReadableIntersectionNameFromCornerID(_intersectionsToKeep[0]),
            GetReadableIntersectionNameFromCornerID(_intersectionsToKeep[1]),
            GetReadableIntersectionNameFromCornerID(_intersectionsToKeep[2]));


        // Offset the first gathered Intersection down by 2
        crystalsToPress[0] = CrystalsKeysTable[((_intersectionIndices[_intersectionsToKeep[0]] + 14) % 70)] - 1;
        // Do not offset the second Intersection
        crystalsToPress[1] = CrystalsKeysTable[_intersectionIndices[_intersectionsToKeep[1]]] - 1;
        // Offset the third gathered Intersection down by 5
        crystalsToPress[2] = CrystalsKeysTable[((_intersectionIndices[_intersectionsToKeep[2]] + 35) % 70)] - 1;

        // Make sure none of them are the same
        if (crystalsToPress[0] == crystalsToPress[1])
        {
            crystalsToPress[1] = (crystalsToPress[1] + 1) % 7;
        }
        if (crystalsToPress[0] == crystalsToPress[2] || crystalsToPress[1] == crystalsToPress[2])
        {
            crystalsToPress[2] = (crystalsToPress[2] + 1) % 7;
            // Crystal 2 can be offset up to twice, if Crystals are 5 4 4 then we end up with 5 4 5 and then 5 4 6
            if (crystalsToPress[0] == crystalsToPress[2] || crystalsToPress[1] == crystalsToPress[2])
            {
                crystalsToPress[2] = (crystalsToPress[2] + 1) % 7;
            }
        }

        Debug.LogFormat("[Toxic Crystals #{0}] Newly generated Final Crystals to press after shifting down: numbers {1}, {2} and {3} in this order.", moduleId,
            crystalsToPress[0] + 1, crystalsToPress[1] + 1, crystalsToPress[2] + 1);
        currentCrystalToPressIndex = 0;
    }

    string GetReadableIntersectionNameFromCornerID(int cornerID)
    {
        switch (cornerID)
        {
            case 0:
                return "Top Left";
            case 1:
                return "Top Center";
            case 2:
                return "Top Right";
            case 3:
                return "Middle Left";
            case 4:
                return "Middle Center";
            case 5:
                return "Middle Right";
            case 6:
                return "Bottom Left";
            case 7:
                return "Bottom Center";
            case 8:
                return "Bottom Right";
            default:
                return "Unknown!";
        }
    }



    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Visuals & Feedbacks Functions
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void InitializeBackgroundAnimation()
    {
        currentBackgroundAnimationTimer = UnityEngine.Random.Range(0f, 5f);
    }



    // We're not centralizing all Toxic Crystals modules' code,
    // But also having all non-centralized modules being synchronized sounds like wasted work
    // So we're gonna randomize offsets and speeds for each instance of the Module
    // They all are gonna do their own work, so let's make it visually interesting!!
    void UpdateBackgroundCasingAnimation()
    {
        currentBackgroundAnimationTimer += Time.deltaTime * backgroundAnimationSpeed;

        // This little "Clamp(%)" allows the timer to be within [0-1] but staying at 1 for a while
        float _modulodTimer = Mathf.Clamp(currentBackgroundAnimationTimer % scaledTimeBetweenBackgroundAnimations, 0f, 1f);

        // Overshoot easing looks bad, I tried it
        float _uvLerp = Easing.InOutCubic(_modulodTimer, 0f, 1f, 1f);
        backgroundCasingMaterial.SetFloat("_UvOffset", _uvLerp);
    }


    void WiggleCrystalsAround()
    {
        // Update Timer
        crystalsMovementTimer += Time.deltaTime * crystalsWiggleMovementSpeed;

        // Pre-prepare a bunch of values
        float sin1 = Mathf.Sin(crystalsMovementTimer);
        float sin2 = sin1 * sin1;
        float sin3 = sin1 * sin2;

        float cos1 = Mathf.Cos(crystalsMovementTimer);
        float cos2 = cos1 * cos1;
        float cos3 = cos1 * cos2;

        // Move Crystals only if they haven't been pressed
        // Yes, those are entirely random
        if (!pressedCrystalsIndices.Contains(0))
        {
            pressableCrystals[0].transform.localPosition = startingCrystalPositions[0] + (new Vector3(sin3 - sin2 + cos2, 0, cos1 - cos3) * crystalsWiggleMovementIntensity);
            pressableCrystals[0].transform.localEulerAngles = startingCrystalRotations[0] + (new Vector3(0, sin1, cos1) * crystalsWiggleRotationIntensity);
        }

        if (!pressedCrystalsIndices.Contains(1))
        {
            pressableCrystals[1].transform.localPosition = startingCrystalPositions[1] + (new Vector3(cos2 - sin1, 0, sin2 + sin3 - cos1) * crystalsWiggleMovementIntensity);
            pressableCrystals[1].transform.localEulerAngles = startingCrystalRotations[1] + (new Vector3(sin2, cos3, sin1) * crystalsWiggleRotationIntensity);
        }
        
        if (!pressedCrystalsIndices.Contains(2))
        {
            pressableCrystals[2].transform.localPosition = startingCrystalPositions[2] + (new Vector3(cos3 + cos1, 0, cos1 + sin3 + cos3) * crystalsWiggleMovementIntensity);
            pressableCrystals[2].transform.localEulerAngles = startingCrystalRotations[2] + (new Vector3(sin2 + cos3, 0, cos2) * crystalsWiggleRotationIntensity);
        }
        
        if (!pressedCrystalsIndices.Contains(3))
        {
            pressableCrystals[3].transform.localPosition = startingCrystalPositions[3] + (new Vector3(sin2, 0, sin3 + sin1 - cos2) * crystalsWiggleMovementIntensity);
            pressableCrystals[3].transform.localEulerAngles = startingCrystalRotations[3] + (new Vector3(cos3, 0, sin1 + cos1) * crystalsWiggleRotationIntensity);
        }
        
        if (!pressedCrystalsIndices.Contains(4))
        {
            pressableCrystals[4].transform.localPosition = startingCrystalPositions[4] + (new Vector3(sin1 - cos1 + cos3, 0, sin3 + cos1 - sin1) * crystalsWiggleMovementIntensity);
            pressableCrystals[4].transform.localEulerAngles = startingCrystalRotations[4] + (new Vector3(cos2 + sin1, sin3, 0) * crystalsWiggleRotationIntensity);
        }
        
        if (!pressedCrystalsIndices.Contains(5))
        {
            pressableCrystals[5].transform.localPosition = startingCrystalPositions[5] + (new Vector3(cos3 + sin3, 0, cos2 - sin3 + sin1) * crystalsWiggleMovementIntensity);
            pressableCrystals[5].transform.localEulerAngles = startingCrystalRotations[5] + (new Vector3(sin3 - cos1, cos2, 0) * crystalsWiggleRotationIntensity);
        }
        
        if (!pressedCrystalsIndices.Contains(6))
        {
            pressableCrystals[6].transform.localPosition = startingCrystalPositions[6] + (new Vector3(cos1 + sin3, 0, sin3 + cos2 - sin1) * crystalsWiggleMovementIntensity);
            pressableCrystals[6].transform.localEulerAngles = startingCrystalRotations[6] + (new Vector3(cos1 + cos3, 0, sin2) * crystalsWiggleRotationIntensity);
        }
        
    }


    void UpdateCasingLetterVisual()
    {
        casingLetterTextMesh.text = currentCasingLetter;
    }


    void UpdateConcentratedCrystalsHue()
    { 
        concentratedCrystalMaterialHueTimer += Time.deltaTime * concentratedCrystalMaterialHueSpeed;

        concentratedCrysalMaterial.SetColor("_Color", Color.HSVToRGB(concentratedCrystalMaterialHueTimer%1, .45f, .82f));
    }




    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Button Pressing Functions
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=


    void CrystalButtonGetsPressed(KMSelectable pressedButton, int crystalIndex)
    {
        if (moduleSolved)
        { return; }

        // Can't press the same button twice
        if (pressedCrystalsIndices.Contains(crystalIndex))
        { return; }


        // Bomb Movement when pressing
        pressedButton.AddInteractionPunch(1f);

        

        if (crystalIndex == crystalsToPress[currentCrystalToPressIndex])
        {
            currentCrystalToPressIndex++;

            Debug.LogFormat("[Toxic Crystals #{0}] Pressed Crystal number {1}. That is a correct Final Crystal!", moduleId, crystalIndex + 1);

            // Do only on the first correct press
            if (pressedCrystalsIndices.Count == 0)
            {
                StopCoroutine(checkForSolvedCoroutine);
                Debug.LogFormat("[Toxic Crystals #{0}] First Final Crystal pressed. New solves will now be ignored.", moduleId);
            }

            // Stop movements for this Crystal
            pressedCrystalsIndices.Add(crystalIndex);
            // Visuals for this button being successfully pressed
            StartCoroutine(BuryPressedCrystal(pressedButton));

            


            if (currentCrystalToPressIndex >= 3)
            {
                // Logic for a Solve

                SolveModule();
                return;
            }

            // Logic for "Good press but not solved"

            // Sound!!
            audioSubmodule.PlaySoundAtTransform(successfulPressSound.name, transform);

        }
        else // Strike
        {
            Debug.LogFormat("[Toxic Crystals #{0}] !! STRIKE !!    Pressed Crystal number {1}. That is incorrect!  !! STRIKE !!", moduleId, crystalIndex + 1);

            thisModule.HandleStrike();
        }

    }

    void SolveModule()
    {
        Debug.LogFormat("[Toxic Crystals #{0}] All three correct Crystals have been pressed. Module Solved!", moduleId);

        moduleSolved = true;

        // Sound!!
        audioSubmodule.PlaySoundAtTransform(moduleSolvedSound.name, transform);

        // Hide letter for Souvenir Support
        casingLetterTextMesh.text = "!";

        thisModule.HandlePass();
    }


    IEnumerator BuryPressedCrystal(KMSelectable CrystalReference)
    {
        Vector3 _startingPos = CrystalReference.transform.localPosition;

        float timer = 0f;
        while (timer < 1)
        {
            timer += Time.deltaTime;
            CrystalReference.transform.localPosition = _startingPos - new Vector3(0, timer * 0.01f, 0);
            yield return null;
        }

        yield return null;
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Twitch Plays
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=


#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"“!{0} Press 1 6 3” to press Crystals 1, 6 and 3 in this order.";
#pragma warning restore 414

    KMSelectable[] ProcessTwitchCommand(string command)
    {
        // Credit to Royal_Flu$h for this line 
        var commandParts = command.ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        if (commandParts.Length < 2)
        {
            return null;
        }

        if (!commandParts[0].Equals("press"))
        {
            return null;
        }

        List<KMSelectable> _CrystalsToPress = new List<KMSelectable>();

        foreach (string _individualCommand in commandParts)
        {
            switch (_individualCommand)
            {
                case "1":
                    _CrystalsToPress.Add(pressableCrystals[0]);
                    break;


                case "2":
                    _CrystalsToPress.Add(pressableCrystals[1]);
                    break;


                case "3":
                    _CrystalsToPress.Add(pressableCrystals[2]);
                    break;


                case "4":
                    _CrystalsToPress.Add(pressableCrystals[3]);
                    break;


                case "5":
                    _CrystalsToPress.Add(pressableCrystals[4]);
                    break;

                case "6":
                    _CrystalsToPress.Add(pressableCrystals[5]);
                    break;
                
                case "7":
                    _CrystalsToPress.Add(pressableCrystals[6]);
                    break;

                default:
                    break;
            }
        }

        return _CrystalsToPress.ToArray();
    }


    // Auto-solve if Twitch Plays needs to force a solve
    IEnumerator TwitchHandleForcedSolve()
    {
        Debug.LogFormat("[Toxic Crystals #{0}] Received ForceSolve via Twitch Command. Auto-solving Module", moduleId);

        for (int i = currentCrystalToPressIndex; i < 3; i ++)
        {
            pressableCrystals[crystalsToPress[i]].OnInteract();
            yield return new WaitForSeconds(0.3f);
        }
    }
}
