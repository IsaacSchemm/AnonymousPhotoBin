class FileModel {
    readonly fileMetadataId: string;
    readonly width: number;
    readonly height: number;
    readonly takenAt: Date | null;
    readonly uploadedAt: Date;
    readonly originalFilename: string;
    readonly userName: KnockoutObservable<string>;
    readonly category: KnockoutObservable<string>;
    readonly size: number;
    readonly contentType: string;

    readonly url: string;
    readonly thumbnailUrl: string;
    readonly newFilename: string;

    checked: KnockoutObservable<boolean>;

    readonly takenOrUploadedAt: Date;
    readonly sizeStr: string;

    readonly takenAtStr: KnockoutComputed<string | null>;
    readonly uploadedAtStr: KnockoutComputed<string>;

    constructor(readonly parent: ListViewModel, m: IFileMetadata) {
        this.fileMetadataId = m.fileMetadataId;
        this.width = m.width;
        this.height = m.height;
        this.takenAt = m.takenAt instanceof Date ? m.takenAt
            : m.takenAt != null ? new Date(m.takenAt)
            : null;
        this.uploadedAt = m.uploadedAt instanceof Date
            ? m.uploadedAt
            : new Date(m.uploadedAt);
        this.takenOrUploadedAt = this.takenAt || this.uploadedAt;
        this.originalFilename = m.originalFilename;
        this.userName = ko.observable(m.userName || "");
        this.category = ko.observable(m.category || "");
        this.size = m.size;
        this.sizeStr = this.size >= 1048576 ? `${(this.size / 1048576).toFixed(2)} MiB`
            : this.size >= 1024 ? `${(this.size / 1024).toFixed(2)} KiB`
                : `${this.size} bytes`;
        this.contentType = m.contentType;
        this.newFilename = m.newFilename;

        this.url = m.url;
        this.thumbnailUrl = m.thumbnailUrl;
        this.checked = ko.observable(false);

        this.takenAtStr = ko.pureComputed(() => this.takenAt && this.takenAt.toLocaleString());
        this.uploadedAtStr = ko.pureComputed(() => this.uploadedAt && this.uploadedAt.toLocaleString());
    }

    toggle(x: FileModel, e: JQuery.Event) {
        if (e.shiftKey) {
            let index1 = Math.max(0, this.parent.files.indexOf(this.parent.lastClicked));
            let index2 = Math.max(0, this.parent.files.indexOf(this));
            let min = Math.min(index1, index2);
            let max = Math.max(index1, index2);
            this.parent.files().forEach((f, i) => {
                f.checked(i >= min && i <= max);
            });
        } else if (e.ctrlKey) {
            this.checked(!this.checked());
            this.parent.lastClicked = this;
        } else {
            this.parent.files().forEach(f => {
                f.checked(f === this);
            });
            this.parent.lastClicked = this;
        }
    }

    defaultAction(f: FileModel, e: Event) {
        return true;
    }
}

interface KnockoutObservableArray<T> {
    orderField: KnockoutObservable<string>;
    orderDirection: KnockoutObservable<"asc" | "desc">;
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

    public lastClicked: FileModel;
    private password: string | null;

    constructor() {
        this.files = ko.observableArray();
        this.lastClicked = this.files()[0];

        try {
            this.myTimeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
        } catch (e) {
            this.myTimeZone = `UTC + ${new Date().getTimezoneOffset()} minutes`;
        }

        this.startDate = ko.observable("");
        this.endDate = ko.observable("");
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

        this.password = null;
    }

    async loadFiles() {
        let resp = await this.fetchOrError("/api/files");
        let files: IFileMetadata[] = await resp.json();
        this.files(files.map(f => new FileModel(this, f)));
        this.resetFilters();
    }

    private async getPassword() {
        if (this.password == null) {
            const p = await promptAsync("Enter the file management password to make changes.");
            if (p != null) {
                const response = await fetch("/api/password/check", {
                    headers: { "X-FileManagementPassword": p },
                    method: "GET"
                });
                if (Math.floor(response.status / 100) == 2) {
                    this.password = p;
                } else {
                    console.error(`${response.status} ${response.statusText}: ${await response.text()}`);
                    await alertAsync(response.status == 400 ? "The password is not valid." : "An unknown error occurred.");
                }
            }
        }
        return this.password;
    }

    resetFilters() {
        let dates = this.files().map(f => f.takenOrUploadedAt).sort((a, b) => +a - +b);
        if (dates.length == 0) {
            this.startDate("2000-01-01T00:00");
            this.endDate("2025-01-01T00:00");
        } else {
            this.startDate(`${dates[0].getFullYear()}-01-01T00:00`);
            this.endDate(`${dates[dates.length - 1].getFullYear() + 1}-01-01T00:00`);
        }
        this.userName("");
        this.category("");
    }

    private async fetchOrError(input: RequestInfo, init?: RequestInit) {
        const response = await fetch(input, {
            ...init,
            headers: {
                'X-FileManagementPassword': await this.getPassword() || "",
                ...(init ? init.headers : {})
            }
        });
        if (Math.floor(response.status / 100) != 2) {
            throw new Error(`${response.status} ${response.statusText}: ${await response.text()}`);
        }
        return response;
    }

    download() {
        const f = $("<form></form>")
            .attr("method", "post")
            .attr("action", "/api/files/zip")
            .appendTo(document.body);
        $("<textarea></textarea>")
            .attr("name", "ids")
            .text(this.selectedFiles().map(f => f.fileMetadataId).join(","))
            .appendTo(f);
        f.submit().remove();
    }

    async changeUserName() {
        if (await this.getPassword() == null) return;
        const newName = await promptAsync("What \"taken by\" name should be listed for these files?", this.selectedFiles()[0].userName());
        if (newName != null) {
            try {
                for (const f of this.selectedFiles()) {
                    const response = await this.fetchOrError(f.url, {
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        method: "PATCH",
                        body: JSON.stringify({ userName: newName })
                    });
                    f.userName(newName);
                    f.checked(false);
                }
            } catch (e) {
                console.error(e);
                await alertAsync("An unknown error occurred.");
            }
        }
    }

    async changeCategory() {
        if (await this.getPassword() == null) return;
        const newCategory = await promptAsync("What category should these files be part of?", this.selectedFiles()[0].category());
        if (newCategory != null) {
            try {
                for (const f of this.selectedFiles()) {
                    const response = await this.fetchOrError(f.url, {
                        headers: {
                            'Content-Type': 'application/json',
                        },
                        method: "PATCH",
                        body: JSON.stringify({ category: newCategory })
                    });
                    f.category(newCategory);
                    f.checked(false);
                }
            } catch (e) {
                console.error(e);
                await alertAsync("An unknown error occurred.");
            }
        }
    }

    async del() {
        if (await this.getPassword() == null) return;
        if (await confirmAsync(`Are you sure you want to permanently delete ${this.selectedFiles().length} file(s) from the server?`)) {
            try {
                for (const f of this.selectedFiles()) {
                    const response = await this.fetchOrError(f.url, {
                        method: "DELETE"
                    });
                    this.files.remove(f);
                }
            } catch (e) {
                console.error(e);
                await alertAsync("An unknown error occurred.");
            }
        }
    }
}

var vm: ListViewModel;

document.addEventListener("DOMContentLoaded", async () => {
    ko.applyBindings(vm = new ListViewModel(), document.getElementById("ko-area"));
    vm.files.orderField("takenOrUploadedAt");
    vm.loadFiles();
}, false);
