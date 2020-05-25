
var contentURL = "https://raw.githubusercontent.com/willem88836/willem88836.github.io/master/resources/";

function httpGet(theUrl)
{
    var xmlHttp = null;
    xmlHttp = new XMLHttpRequest();
    xmlHttp.open( "GET", theUrl, false );
    xmlHttp.send( null );
    return xmlHttp.responseText;
}


function loadRawContent(elementId, contentPath) {
    document.getElementById(elementId).innerHTML = httpGet(contentURL + contentPath);
}


function initializeResume() {
    loadRawContent("short-resume", "texts/short-resume.html");
}

function initializeProblemSolver(){
    loadRawContent("spraac-description", "projects/spraac-description.html");
    loadRawContent("intoreality-description", "projects/intoreality-description.html");
    loadRawContent("samenwerkingsspel-description", "projects/samenwerkingsspel-description.html");
}

function initializeDeveloper() {
    loadRawContent("short-developer", "texts/short-developer.html");
    
    loadRawContent("code-display-py", "sourcecode/GameOfLife.py")
    loadRawContent("commentary-game-of-life", "sourcecode/commentary-GameOfLife.html");

    loadRawContent("code-display-cs", "sourcecode/QuickSort.cs");
    loadRawContent("commentary-quick-sort", "sourcecode/commentary-QuickSort.html");

    loadRawContent("code-display-java", "sourcecode/KruskalsAlgorithm.java");
    loadRawContent("commentary-kruskals-algorithm", "sourcecode/commentary-KruskalsAlgorithm.html");
}
