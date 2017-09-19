interface ServerFile {
    
}

class ListViewModel {
    constructor(files: ServerFile[]) {
        
    }
}

document.addEventListener("DOMContentLoaded", async () => {
    /*let resp = await fetch("/api/files");
    let files = await resp.json();
    for (const file of files) {
        file.checked = ko.observable(false);
    }
    ko.applyBindings({ files: files }, document.getElementById("ko-area"));*/
}, false);
