﻿interface IFileMetadata {
    fileMetadataId: string;
    width: number;
    height: number;
    takenAt: Date | string | null;
    uploadedAt: Date | string;
    originalFilename: string;
    userName: string | null;
    category: string | null;
    contentType: string;
    newFilename: string;
}

class FileModel implements IFileMetadata {
    readonly fileMetadataId: string;
    readonly width: number;
    readonly height: number;
    readonly takenAt: Date | null;
    readonly uploadedAt: Date;
    readonly originalFilename: string;
    readonly userName: string;
    readonly category: string;
    readonly contentType: string;
    readonly newFilename: string;

    readonly url: string;
    readonly thumbnailUrl: string;
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
        this.userName = m.userName || "";
        this.category = m.category || "";
        this.contentType = m.contentType;
        this.newFilename = m.newFilename;

        this.url = `/api/files/${this.fileMetadataId}`;
        this.thumbnailUrl = `/api/thumbnails/${this.fileMetadataId}`;
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
    readonly files: FileModel[];
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
        this.files = files.map(f => new FileModel(f)).sort((a, b) => +(a.takenAt || a.uploadedAt) - +(b.takenAt || b.uploadedAt));

        try {
            this.myTimeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
        } catch (e) {
            this.myTimeZone = `UTC + ${new Date().getTimezoneOffset()} minutes`;
        }

        this.startDate = ko.observable("2000-01-01T00:00");
        this.endDate = ko.observable(`${new Date().getFullYear() + 1}-01-01T00:00`);
        this.userName = ko.observable("");
        this.category = ko.observable("");
        this.viewStyle = ko.observable("Table");

        this.selectAllChecked = ko.observable(false);
        this.selectAllChecked.subscribe(newValue => {
            for (const f of this.displayedFiles()) {
                f.checked(newValue);
            }
        });

        this.selectedFiles = ko.pureComputed(() => this.files.filter(f => f.checked()));

        this.userNames = ko.pureComputed(() => [""].concat(this.files.map(f => f.userName))
            .filter((v, i, a) => a.indexOf(v) === i)
            .sort());
        this.categories = ko.pureComputed(() => [""].concat(this.files.map(f => f.category))
            .filter((v, i, a) => a.indexOf(v) === i)
            .sort());
        this.displayedFiles = ko.pureComputed(() => this.files.filter(f => {
            if (this.startDate() && new Date(this.startDate()) > (f.takenAt || f.uploadedAt)) return false;
            if (this.endDate() && new Date(this.endDate()) < (f.takenAt || f.uploadedAt)) return false;
            if (this.userName() && f.userName != this.userName()) return false;
            if (this.category() && f.category != this.category()) return false;
            return true;
        }));

        this.undisplayedSelectedFiles = ko.pureComputed(() => {
            const displayedFiles = this.displayedFiles();
            return this.selectedFiles().filter(f => displayedFiles.indexOf(f) < 0);
        });
    }

    download() {
        
    }

    changeUserName() {
        const newName = prompt("What \"taken by\" name should be listed for these files?", this.selectedFiles()[0].userName);
        if (newName != null) {

        }
    }

    changeCategory() {
        const newCategory = prompt("What category should these files be part of?", this.selectedFiles()[0].category);
        if (newCategory != null) {

        }
    }

    del() {
        if (confirm(`Are you sure you want to delete ${this.selectedFiles().length} file(s)?`)) {

        }
    }
}

var vm: ListViewModel;

document.addEventListener("DOMContentLoaded", async () => {
    let resp = await fetch("/api/files");
    let files = await resp.json();
    ko.applyBindings(vm = new ListViewModel(files), document.getElementById("ko-area"));
}, false);
