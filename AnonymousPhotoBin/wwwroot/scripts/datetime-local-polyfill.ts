declare var bootbox: any;

interface JQuery {
    datetimepicker(o: any): any;
    applyDateTimeLocalPolyfill(): void;
}

jQuery.fn.applyDateTimeLocalPolyfill = async function (this: JQuery) {
    $("input[type=datetime-local]", this).each(function (index, element) {
        if (element instanceof HTMLInputElement && element.type !== "datetime-local") {
            element.type = "text";
            element.addEventListener("click", e => {
                e.preventDefault();

                $(element).blur();

                const defaultDate = new Date();
                defaultDate.setHours(12, 0, 0, 0);

                let currentDate: Date | string = element.value || defaultDate;

                const box = bootbox.confirm({
                    message: "<div class='calendar-placeholder' type='text' style='width: 384px; height: 218px'></div>",
                    callback: (b: boolean) => {
                        if (b) {
                            const z = (t: number) => {
                                let s = `${t}`;
                                while (s.length < 2) s = `0${s}`;
                                return s;
                            }
                            const asString = typeof currentDate === "string"
                                ? currentDate
                                : `${currentDate.getFullYear()}-${z(currentDate.getMonth() + 1)}-${z(currentDate.getDate())}T${z(currentDate.getHours())}:${z(currentDate.getMinutes())}:${z(currentDate.getSeconds())}`;
                            $(element).val(asString).trigger("change").trigger("blur");
                        }
                    }
                });
                $(".modal-dialog", box).css("width", "340px");

                setTimeout(() => {
                    $(".calendar-placeholder", box).datetimepicker({
                        inline: true,
                        step: 15,
                        formatTime: 'g:i A',
                        value: currentDate,
                        onChangeDateTime: (date: Date) => currentDate = date
                    });
                }, 0);
            });
        }
    });
}