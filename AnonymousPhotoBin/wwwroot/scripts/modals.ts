interface Window {
    alertify: any;
    bootbox: any;
    BootstrapDialog: any;
}

function alertAsync(str: string): Promise<void> | void {
    return window.bootbox ? new Promise(resolve => window.bootbox.alert({
        message: str,
        callback: () => resolve(),
        animate: false
    }))
    : window.alertify ? new Promise(resolve => window.alertify.alert(str, () => resolve()))
    : window.BootstrapDialog ? new Promise(resolve => window.BootstrapDialog.show({
        message: $("<div></div>").text(str).html(),
        closable: false,
        buttons: [
            {
                label: "OK",
                hotkey: 13,
                action: (d: any) => {
                    d.close();
                    resolve();
                }
            }
        ]
    }))
    : alert(str);
}

function promptAsync(str: string, _default?: string): Promise<string | null> | string | null {
    return window.bootbox ? new Promise(resolve => window.bootbox.prompt({
        title: str,
        value: _default,
        callback: (s: string) => resolve(s),
        animate: false
    }))
    : window.alertify ? new Promise(resolve => window.alertify.prompt(str, _default, (e: Event, s: string) => resolve(s)))
    : window.BootstrapDialog ? new Promise<string | null>(resolve => window.BootstrapDialog.show({
        message: $("<div></div>")
            .text(str)
            .append("<br/>")
            .append($("<input/>").attr("type", "text").val(_default || "").css("width", "100%"))
            .html(),
        closable: false,
        buttons: [
            {
                label: "OK",
                hotkey: 13,
                action: (d: any) => {
                    d.close();
                    resolve(d.getModalBody().find("input").val());
                }
            }, {
                label: "Cancel",
                action: (d: any) => {
                    d.close();
                    resolve(null);
                }
            }
        ]
    }))
    : prompt(str, _default);
}

function confirmAsync(str: string): Promise<boolean> | boolean {
    return window.bootbox ? new Promise(resolve => window.bootbox.confirm({
        message: str,
        callback: (b: boolean) => resolve(b),
        animate: false
    }))
    : window.alertify ? new Promise(resolve => window.alertify.confirm(str, (e: Event, b: boolean) => resolve(b)))
    : window.BootstrapDialog ? new Promise(resolve => window.BootstrapDialog.show({
        message: $("<div></div>").text(str).html(),
        closable: false,
        buttons: [
            {
                label: "OK",
                hotkey: 13,
                action: (d: any) => {
                    d.close();
                    resolve(true);
                }
            }, {
                label: "Cancel",
                action: (d: any) => {
                    d.close();
                    resolve(false);
                }
            }
        ]
    }))
    : confirm(str);
}
