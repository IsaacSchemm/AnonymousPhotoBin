///<reference path="../../node_modules/@types/jquery/index.d.ts"/>
///<reference path="../../node_modules/@types/jquery.fileupload/index.d.ts"/>

$(() => {
    try {
        let e = document.querySelector("input[name=timezone]");
        if (e instanceof HTMLInputElement) {
            e.value = Intl.DateTimeFormat().resolvedOptions().timeZone;
        }
    } catch (e) {
        console.error(e);
    }

    const datas: JQueryFileInputOptions[] = [];
    var filesUploaded = 0;

    $("form").fileupload({
        url: "/api/files",
        sequentialUploads: true,
        dataType: "json",
        /*add: function (e, data) {
            datas.push(data);
        },*/
        send: function (e, data) {
            $("#files").text(`Uploading ${data.files[0].name}...`);
        },
        done: function (e, data) {
            $("#files").text(`Uploaded ${++filesUploaded} files.`);
        },
        progressall: function (e, data) {
            var progress = (data.loaded || 0) / (data.total || 1) * 100
            $('#progress .progress-bar').css('width', progress + '%');
        }
    })

    $("form").hide();
    $("#chooseFiles").click(function () {
        $("input[type=file]").click();
    });
});