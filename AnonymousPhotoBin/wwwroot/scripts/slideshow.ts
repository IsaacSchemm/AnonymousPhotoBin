interface JQuery {
    swipeshow(options?: {
        autostart?: boolean;
        interval?: number;
        initial?: number;
        speed?: number;
        friction?: number;
        mouse?: boolean;
        keys?: boolean;
        onactivate?: Function;
        onpause?: Function;
    }): JQuery;
}

let cursorTimeout: number;
function showCursor() {
    clearTimeout(cursorTimeout);
    $(".slide").css("cursor", "");
    $("#buttons").show();
    cursorTimeout = setTimeout(() => {
        $(".slide").css("cursor", "none");
        $("#buttons").hide();
    }, 1000);
}

$(async () => {
    try {
        let guid = (/[\?&]id=([^&]+)/.exec(location.search) || [])[1];
        if (!guid) throw new Error("id parameter is invalid");
        let response = await fetch(`/api/slideshow/${guid}`);
        let array: string[] = await response.json();
        for (let s of array) {
            $("<li></li>")
                .addClass("slide")
                .css(`background-image`, `url('/api/files/${s}')`)
                .appendTo("ul");
        }

        showCursor();
        $(".swipeshow").on("mousemove", e => showCursor()).swipeshow({
            autostart: false
        });
    } catch (e) {
        console.error(e);
        alertAsync("An unknown error occurred.");
    }
});
