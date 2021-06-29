
// Scroll to top functionality.
{
    // enables/disables the button based on the current scroll value. 
    function scrollFunction() {
        if (document.body.scrollTop > 20 || document.documentElement.scrollTop > 20) {
        mybutton.style.display = "block";
        } else {
        mybutton.style.display = "none";
        }
    }

    // scrolls to the top of the frame.
    function scrollToTop() {
        document.body.scrollTop = 0; 
        document.documentElement.scrollTop = 0; 
    } 

    mybutton = document.getElementById("to-top-button");
    window.onscroll = function() {scrollFunction()};   
}


// Visual functionality.
{   
    // sets my age dynamically. 
    function setMyAge() {
        var ageField = document.getElementById('my-age');
        var dif_ms = Date.now() - new Date(1998, 02, 02);
        var age = new Date(dif_ms);
        var years = Math.abs(age.getUTCFullYear() - 1970);
        ageField.innerHTML = "(" + years + ")"; 
    }

    // changes header name when on a phone.
    function setMyDisplayName() {
        if (typeof window.orientation !== 'undefined') { 
            var displayName = document.getElementById("display-name");
            displayName.innerHTML = "W. Meijer";
        }
    }

    setMyAge();
    setMyDisplayName();
}
