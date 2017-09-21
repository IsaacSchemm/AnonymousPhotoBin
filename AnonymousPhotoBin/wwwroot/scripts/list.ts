class FileModel {
    readonly fileMetadataId: string;
    readonly width: number;
    readonly height: number;
    readonly takenAt: Date | null;
    readonly uploadedAt: Date;
    readonly originalFilename: string;
    readonly userName: KnockoutObservable<string>;
    readonly category: KnockoutObservable<string>;
    readonly contentType: string;

    readonly url: string;
    readonly thumbnailUrl: string;
    readonly newFilename: string;

    checked: KnockoutObservable<boolean>;

    readonly takenAtStr: KnockoutComputed<string | null>;
    readonly uploadedAtStr: KnockoutComputed<string>;

    constructor(m: IFileMetadata) {
        this.fileMetadataId = m.fileMetadataId;
        this.width = m.width;
        this.height = m.height;
        this.takenAt = m.takenAt != null
            ? new Date(m.takenAt)
            : null;
        this.uploadedAt = new Date(m.uploadedAt);
        this.originalFilename = m.originalFilename;
        this.userName = ko.observable(m.userName || "");
        this.category = ko.observable(m.category || "");
        this.contentType = m.contentType;
        this.newFilename = m.newFilename;

        this.url = m.url;
        this.thumbnailUrl = m.thumbnailUrl;
        this.checked = ko.observable(false);

        this.takenAtStr = ko.pureComputed(() => this.takenAt && this.takenAt.toLocaleString());
        this.uploadedAtStr = ko.pureComputed(() => this.uploadedAt && this.uploadedAt.toLocaleString());
    }

    toggle() {
        this.checked(!this.checked());
    }

    defaultAction(f: FileModel, e: Event) {
        return true;
    }
}

class ListViewModel {
    readonly files: KnockoutObservableArray<FileModel>;
    readonly myTimeZone: string;

    readonly startDate: KnockoutObservable<string>;
    readonly endDate: KnockoutObservable<string>;
    readonly userName: KnockoutObservable<string>;
    readonly category: KnockoutObservable<string>;
    readonly viewStyle: KnockoutObservable<string>;

    readonly selectAllChecked: KnockoutObservable<boolean>;

    readonly selectedFiles: KnockoutComputed<FileModel[]>;

    readonly userNames: KnockoutObservable<string[]>;
    readonly categories: KnockoutObservable<string[]>;
    readonly displayedFiles: KnockoutComputed<FileModel[]>;
    readonly undisplayedSelectedFiles: KnockoutComputed<FileModel[]>;

    constructor(files: IFileMetadata[]) {
        this.files = ko.observableArray(files
            .map(f => new FileModel(f))
            .sort((a, b) => +(a.takenAt || a.uploadedAt) - +(b.takenAt || b.uploadedAt)));

        try {
            this.myTimeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
        } catch (e) {
            this.myTimeZone = `UTC + ${new Date().getTimezoneOffset()} minutes`;
        }

        this.startDate = ko.observable("2000-01-01T00:00");
        this.endDate = ko.observable(`${new Date().getFullYear() + 1}-01-01T00:00`);
        this.userName = ko.observable("");
        this.category = ko.observable("");
        this.resetFilters();
        
        this.viewStyle = ko.observable("table");

        this.selectAllChecked = ko.observable(false);
        this.selectAllChecked.subscribe(newValue => {
            for (const f of this.displayedFiles()) {
                f.checked(newValue);
            }
        });

        this.selectedFiles = ko.pureComputed(() => this.files().filter(f => f.checked()));

        this.userNames = ko.pureComputed(() => [""].concat(this.files().map(f => f.userName()))
            .filter((v, i, a) => a.indexOf(v) === i)
            .sort());
        this.categories = ko.pureComputed(() => [""].concat(this.files().map(f => f.category()))
            .filter((v, i, a) => a.indexOf(v) === i)
            .sort());
        this.displayedFiles = ko.pureComputed(() => this.files().filter(f => {
            if (this.startDate() && new Date(this.startDate()) > (f.takenAt || f.uploadedAt)) return false;
            if (this.endDate() && new Date(this.endDate()) < (f.takenAt || f.uploadedAt)) return false;
            if (this.userName() && f.userName() != this.userName()) return false;
            if (this.category() && f.category() != this.category()) return false;
            return true;
        }));

        this.undisplayedSelectedFiles = ko.pureComputed(() => {
            const displayedFiles = this.displayedFiles();
            return this.selectedFiles().filter(f => displayedFiles.indexOf(f) < 0);
        });
    }

    resetFilters() {
        this.startDate("2000-01-01T00:00");
        this.endDate(`${new Date().getFullYear() + 1}-01-01T00:00`);
        this.userName("");
        this.category("");
    }

    download() {
        
    }

    async changeUserName() {
        const newName = prompt("What \"taken by\" name should be listed for these files?", this.selectedFiles()[0].userName());
        if (newName != null) {
            try {
                for (const f of this.selectedFiles()) {
                    const response = await fetch(f.url, {
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        method: "PATCH",
                        body: JSON.stringify({ userName: newName })
                    });
                    if (Math.floor(response.status / 100) != 2) {
                        throw new Error(`HTTP response ${response.status} ${response.statusText}`);
                    }
                    f.userName(newName);
                    f.checked(false);
                }
            } catch (e) {
                console.error(e);
                alert("An unknown error occurred.");
            }
        }
    }

    async changeCategory() {
        const newCategory = prompt("What category should these files be part of?", this.selectedFiles()[0].category());
        if (newCategory != null) {
            try {
                for (const f of this.selectedFiles()) {
                    const response = await fetch(f.url, {
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        method: "PATCH",
                        body: JSON.stringify({ category: newCategory })
                    });
                    if (Math.floor(response.status / 100) != 2) {
                        throw new Error(`HTTP response ${response.status} ${response.statusText}`);
                    }
                    f.category(newCategory);
                    f.checked(false);
                }
            } catch (e) {
                console.error(e);
                alert("An unknown error occurred.");
            }
        }
    }

    async del() {
        if (confirm(`Are you sure you want to permanently delete ${this.selectedFiles().length} file(s) from the server?`)) {
            try {
                for (const f of this.selectedFiles()) {
                    const response = await fetch(f.url, {
                        method: "DELETE"
                    });
                    if (Math.floor(response.status / 100) != 2) {
                        throw new Error(`HTTP response ${response.status} ${response.statusText}`);
                    }
                    this.files.remove(f);
                }
            } catch (e) {
                console.error(e);
                alert("An unknown error occurred.");
            }
        }
    }
}

var vm: ListViewModel;

document.addEventListener("DOMContentLoaded", async () => {
    let resp = await fetch("/api/files");
    let files = await resp.json();
    ko.applyBindings(vm = new ListViewModel(files), document.getElementById("ko-area"));
}, false);
