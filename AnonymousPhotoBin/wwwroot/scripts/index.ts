﻿///<reference path="../../node_modules/@types/jquery/index.d.ts"/>
///<reference path="../../node_modules/@types/jquery.fileupload/index.d.ts"/>
///<reference path="../../node_modules/@types/knockout/index.d.ts"/>

interface FileData extends JQueryFileInputOptions {
    process: () => JQueryPromise<void>;
    submit: () => JQueryPromise<void>;
}

class FileUploadViewModel {
    readonly files: KnockoutObservableArray<FileData>;
    readonly fileProgress: KnockoutObservable<number>;
    readonly totalProgress: KnockoutObservable<number>;
    readonly caption: KnockoutObservable<string>;
    readonly uploading: KnockoutObservable<boolean>;

    readonly fileProgressPercentage: KnockoutComputed<string>;
    readonly totalProgressPercentage: KnockoutComputed<string>;
    readonly uploadButtonText: KnockoutComputed<string>;

    constructor() {
        this.files = ko.observableArray();
        this.fileProgress = ko.observable(0);
        this.totalProgress = ko.observable(0);
        this.caption = ko.observable("");
        this.uploading = ko.observable(false);

        this.fileProgressPercentage = ko.pureComputed(() => `${this.fileProgress() * 100}%`);
        this.totalProgressPercentage = ko.pureComputed(() => `${this.totalProgress() * 100}%`);
        this.uploadButtonText = ko.pureComputed(() => {
            const l = this.files().length;
            return l == 1 ? "Upload file" : `Upload ${l} files`;
        });

        $("form").fileupload({
            url: "/api/files",
            sequentialUploads: true,
            dataType: "json",
            add: (e, data) => {
                this.files.push(data as FileData);
            },
            progressall: (e, data) => {
                this.fileProgress((data.loaded || 0) / (data.total || 0));
            }
        }).hide();

        ko.applyBindings(this, document.getElementById("ko-area"));
    }

    choose() {
        $("input[type=file]").click();
    }

    async upload() {
        if (this.uploading()) {
            alert("Please wait until the other files are done uploading.");
        }

        this.uploading(true);

        let filesUploaded = 0;
        while (true) {
            const f = this.files.shift();
            if (!f) break;

            try {
                this.caption(`Uploading ${f.files[0].name}...`);
                await f.process();
                await f.submit();

                filesUploaded++;
                this.totalProgress(filesUploaded / (filesUploaded + this.files().length));
            } catch (e) {
                this.files.unshift(f);
                console.error(e);
                alert("An unknown error occurred.");
            }
        }

        this.caption(`Uploaded ${filesUploaded} files.`);
        this.fileProgress(0);
        this.totalProgress(0);
        this.uploading(false);
    }
}

$(() => {
    try {
        let e = document.querySelector("input[name=timezone]");
        if (e instanceof HTMLInputElement) {
            e.value = Intl.DateTimeFormat().resolvedOptions().timeZone;
        }
    } catch (e) {
        console.error(e);
    }

    new FileUploadViewModel();
});