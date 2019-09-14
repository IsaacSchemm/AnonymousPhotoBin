interface FileData {
    files: File[];
    process(): JQueryPromise<void>;
    submit(): JQueryPromise<any>;
}

class FileUploadViewModel {
    readonly files: KnockoutObservableArray<FileData>;
    readonly fileProgress: KnockoutObservable<number>;
    readonly caption1: KnockoutObservable<string | null>;
    readonly totalProgress: KnockoutObservable<number>;
    readonly caption2: KnockoutObservable<string | null>;
    readonly errors: KnockoutObservableArray<string>;
    readonly uploaded: KnockoutObservableArray<IExistingPhoto>;
    readonly showUploaded: KnockoutObservable<boolean>;
    readonly uploading: KnockoutObservable<boolean>;

    readonly filesBeingUploaded: KnockoutObservable<number>;
    readonly fileProgressPercentage: KnockoutComputed<string>;
    readonly totalProgressPercentage: KnockoutComputed<string>;
    readonly progressPercentage: KnockoutComputed<string>;
    readonly uploadButtonText: KnockoutComputed<string>;

    constructor() {
        this.files = ko.observableArray();
        this.fileProgress = ko.observable(0);
        this.totalProgress = ko.observable(0);
        this.filesBeingUploaded = ko.observable(0);
        this.caption1 = ko.observable(null);
        this.caption2 = ko.observable(null);
        this.errors = ko.observableArray();
        this.uploaded = ko.observableArray();
        this.showUploaded = ko.observable(false);
        this.uploading = ko.observable(false);

        this.fileProgressPercentage = ko.pureComputed(() => `${this.fileProgress() * 100}%`);
        this.totalProgressPercentage = ko.pureComputed(() => `${this.totalProgress() * 100}%`);
        this.progressPercentage = ko.pureComputed(() => `${((this.fileProgress() / this.filesBeingUploaded()) + this.totalProgress()) * 100}%`);
        this.uploadButtonText = ko.pureComputed(() => {
            const l = this.files().length;
            return l == 1 ? "Upload file" : `Upload ${l} files`;
        });

        $("form").fileupload({
            url: "/api/files",
            sequentialUploads: true,
            dataType: "json",
            add: (e, data: FileData) => {
                let tooBig = data.files.filter(f => f.size > 105000000);
                if (tooBig.length > 0) {
                    alertAsync(`The file "${tooBig[0].name}" cannot be uploaded because it is bigger than 200 MB.`);
                } else {
                    this.files.push(data);
                }
            },
            progressall: (e, data) => {
                this.fileProgress((data.loaded || 0) / (data.total || 0));
            }
        });
        $("form").submit(e => {
            e.preventDefault();
            this.upload();
        })
        $("input[type=file], input[type=submit]").hide();

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
        let bar = 0;
        let data: FileData | undefined;
        this.caption1("");
        this.caption2("");
        this.errors([]);
        this.filesBeingUploaded(files.length);
        for (let data of files) {
            try {
                this.caption1(`Uploading ${data.files[0].name}...`);

                await data.process();
                let result: IExistingPhoto[] = await data.submit();
                
                for (const f of result) {
                    this.uploaded.unshift(f);
                }

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
            this.totalProgress(++bar / files.length);
        }

        this.caption2("Upload complete.");
        this.fileProgress(0);
        this.totalProgress(0);
        this.filesBeingUploaded(0);
        this.uploading(false);
    }

    reset() {
        this.files([]);
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