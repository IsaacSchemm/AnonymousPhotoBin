interface FileData {
    files: File[];
    process(): JQueryPromise<void>;
    submit(): JQueryPromise<any>;
}

class FileUploadViewModel {
    readonly files: KnockoutObservableArray<FileData>;
    readonly individualFileProgress: KnockoutObservable<number>;
    readonly otherFilesProgress: KnockoutObservable<number>;
    readonly caption1: KnockoutObservable<string | null>;
    readonly caption2: KnockoutObservable<string | null>;
    readonly errors: KnockoutObservableArray<string>;
    readonly uploading: KnockoutObservable<boolean>;

    readonly filesBeingUploaded: KnockoutObservable<number>;
    readonly totalProgress: KnockoutComputed<number>;
    readonly uploadButtonText: KnockoutComputed<string>;

    constructor() {
        this.files = ko.observableArray();
        this.individualFileProgress = ko.observable(0);
        this.otherFilesProgress = ko.observable(0);
        this.filesBeingUploaded = ko.observable(1);
        this.caption1 = ko.observable(null);
        this.caption2 = ko.observable(null);
        this.errors = ko.observableArray();
        this.uploading = ko.observable(false);

        this.totalProgress = ko.pureComputed(() => (this.individualFileProgress() / this.filesBeingUploaded()) + this.otherFilesProgress());
        this.uploadButtonText = ko.pureComputed(() => {
            const l = this.files().length;
            return l == 1 ? "Upload file" : `Upload ${l} files`;
        });

        $("main form").fileupload({
            url: "/api/files",
            sequentialUploads: true,
            dataType: "json",
            add: (_, data: FileData) => {
                let tooBig = data.files.filter(f => f.size > 105000000);
                if (tooBig.length > 0) {
                    alertAsync(`The file "${tooBig[0].name}" cannot be uploaded because it is bigger than 200 MB.`);
                } else {
                    this.files.push(data);
                }
            },
            progressall: (_, data) => {
                this.individualFileProgress((data.loaded || 0) / (data.total || 1));
            }
        });
        $("main form").on("submit", e => {
            e.preventDefault();
            this.upload();
        })
        $("main input[type=file], main input[type=submit]").hide();

        ko.applyBindings(this, document.getElementById("ko-area"));
    }

    choose() {
        $("main input[type=file]").trigger("click");
    }

    async upload() {
        if (this.uploading()) {
            alert("Please wait until the other files are done uploading.");
        }

        this.uploading(true);

        const files = this.files();
        this.files([]);

        let filesUploaded = 0;
        let bar = 0;
        this.caption1("");
        this.caption2("");
        this.errors([]);
        this.individualFileProgress(0);
        this.otherFilesProgress(0);
        this.filesBeingUploaded(files.length);
        for (let data of files) {
            try {
                this.caption1(`Uploading ${data.files[0].name}...`);

                await data.process();
                let result: IFileMetadata[] = await data.submit();

                filesUploaded++;
            } catch (e) {
                console.error(e);
                if ("status" in e && e.status == 502) {
                    this.errors.push(`Could not save ${data.files[0].name} (a database error occurred.)`);
                } else {
                    this.errors.push(`Could not save ${data.files[0].name} (an unknown error occurred.)`);
                }
            }
            this.caption1("");
            this.individualFileProgress(0);
            this.otherFilesProgress(++bar / files.length);
        }

        this.caption2("Upload complete.");
        this.otherFilesProgress(0);
        this.uploading(false);
    }

    reset() {
        this.files([]);
    }
}

if ("$" in window) {
    $(() => {
        new FileUploadViewModel();
    });
}