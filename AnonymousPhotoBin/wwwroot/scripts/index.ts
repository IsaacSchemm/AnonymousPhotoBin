interface FileData extends JQueryFileInputOptions {
    process: () => JQueryPromise<void>;
    submit: () => JQueryPromise<any>;
}

interface UploadedFileResult {
    url: string;
    thumbnailUrl: string;
    name: string;
}

class FileUploadViewModel {
    readonly files: KnockoutObservableArray<FileData>;
    readonly fileProgress: KnockoutObservable<number>;
    readonly caption1: KnockoutObservable<string | null>;
    readonly totalProgress: KnockoutObservable<number>;
    readonly caption2: KnockoutObservable<string | null>;
    readonly uploaded: KnockoutObservableArray<UploadedFileResult>;
    readonly uploading: KnockoutObservable<boolean>;

    readonly fileProgressPercentage: KnockoutComputed<string>;
    readonly totalProgressPercentage: KnockoutComputed<string>;
    readonly uploadButtonText: KnockoutComputed<string>;

    constructor() {
        this.files = ko.observableArray();
        this.fileProgress = ko.observable(0);
        this.totalProgress = ko.observable(0);
        this.caption1 = ko.observable(null);
        this.caption2 = ko.observable(null);
        this.uploaded = ko.observableArray();
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

        const files = this.files();
        this.files([]);

        let filesUploaded = 0;
        let data: FileData | undefined;
        for (let data of files) {
            try {
                this.caption1(`Uploading ${data.files[0].name}...`);

                await data.process();
                let result = await data.submit();

                if (result) {
                    for (const f of result) {
                        this.uploaded.unshift(f);
                    }
                }

                filesUploaded++;
                this.caption1("");
                this.caption2(`Uploaded ${filesUploaded} file${filesUploaded == 1 ? "" : "s"}.`);
                this.totalProgress(filesUploaded / (filesUploaded + this.files().length));
            } catch (e) {
                console.error(e);
                this.caption1(`An unknown error occurred while uploading ${data!.files[0].name}.`);
                break;
            }
        }

        this.fileProgress(0);
        this.totalProgress(0);
        this.uploading(false);
    }

    clearUploaded() {
        this.uploaded([]);
    }
}

if ("$" in window) {
    $(() => {
        new FileUploadViewModel();
    });
}