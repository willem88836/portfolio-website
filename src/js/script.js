
var contentURL = "https://raw.githubusercontent.com/willem88836/willem88836.github.io/master/resources/";

function httpGet(theUrl)
{
    var xmlHttp = null;
    xmlHttp = new XMLHttpRequest();
    xmlHttp.open( "GET", theUrl, false );
    xmlHttp.send( null );
    return xmlHttp.responseText;
}
