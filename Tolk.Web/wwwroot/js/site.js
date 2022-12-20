// Write your JavaScript code.

var urlParams = undefined;

// Set up date-picker. See docs at https://bootstrap-datepicker.readthedocs.io/en/latest/markup.html
var datePickerOptions = {
    language: 'sv',
    calendarWeeks: true,
    todayHighlight: true,
    clearBtn: true,
    orientation: "bottom",
    format: {
        toDisplay: function (date, format, language) {
            return date.toISOString().slice(0, 10);
        },
        toValue: function (date, format, language) {
            return new Date(Date.customFormat(date));
        }
    }
};

$('#impersonation-select').change(function () {
    $("#impersonation-form").submit();
});

$('#timeTravelDatePicker').on('changeDate', function () {
    $('#timeTravelDate').val(
        $('#timeTravelDatePicker').datepicker('getFormattedDate')
    );
});

function getAntiForgeryToken() {
    return $('input[name="__RequestVerificationToken"]').val();
}

function updateTime() {
    var date = new Date(new Date().getTime() + Number($('#now').attr('data-timetravel-milliseconds')));
    $('#now').val(date);
    $('#now_display').text(date.toLocaleString("sv-SE"));
}

if ($('#now').length === 1) {
    updateTime();
    setInterval(updateTime, 1000);
}

function refreshCollapsibles() {
    $(".collapsible-section").each(function () {
        var element = $(this);
        if (!element.hasClass("disabled")) {
            if ($(element.attr("data-target")).hasClass("in")) {
                element.children("h1").children("i").remove();
                $('<i class="glyphicon glyphicon-triangle-bottom" style="font-size:15px;margin-right:10px;"></i>').prependTo(element.children("h1"));
            }
            else {
                element.children("h1").children("i").remove();
                $('<i class="glyphicon glyphicon-triangle-right" style="font-size:15px;margin-right:10px;"></i>').prependTo(element.children("h1"));
            }
        }
    });
}

function changeTab(element, path, tabPaneSelector) {
    // Load only if tab is empty and enabled
    var isDisabled = element.parentElement.className.indexOf("disabled") !== -1;
    if (!isDisabled && tabPaneSelector !== "#" && $(tabPaneSelector).html().trim().length === 0) {
        $(tabPaneSelector).html('<div class="text-align-center"><span class="loading-text">Laddar...</span></div>');
        $.ajax({
            url: tolkBaseUrl + path,
            type: 'GET',
            dataType: 'html',
            success: function (data) {
                $(tabPaneSelector).html(data);
                $('.form-entry-information').tooltip();
                refreshCollapsibles();
            },
            error: function (t2) {
                alert(t2);
            }
        });
    }
}

function changeRequisition(element, path, tabPaneSelector) {
    $(tabPaneSelector).html('<div class="text-align-center"><span class="loading-text">Laddar...</span></div>');
    $.ajax({
        url: tolkBaseUrl + path,
        type: 'GET',
        dataType: 'html',
        success: function (data) {
            $(tabPaneSelector).html(data);
            $('.form-entry-information').tooltip();
            refreshCollapsibles();
        },
        error: function (t2) {
            alert(t2);
        }
    });
}

$.urlParam = function (name) {
    var results = new RegExp('[\?&]' + name + "=([^&#]*)").exec(window.location.href);
    if (results != null) { //eslint-disable-line eqeqeq
        return decodeURI(results[1]) || 0;
    }
    return null;
};

$(function () {

    var dirty = "dirty";
    switch ($.urlParam('tab')) {
        case 'requisition':
            if ($('#requisitionTab').length) {
                $('#requisitionTab').click();
            }
            break;
        case 'complaint':
            if ($('#complaintTab').length) {
                $('#complaintTab').click();
            }
            break;
        default:
            break;
    }

    $('.form-entry-information').tooltip();

    $(".disable-on-click").closest("form").on("submit", function () {
        $(this).find(".disable-on-click").disableOnSubmit();
    });

    var orderDatePickerOptions = jQuery.extend({}, datePickerOptions);
    orderDatePickerOptions.startDate = new Date($('#now').val()).zeroTime();
    $('.datepicker').not('.order-datepicker .datepicker').datepicker(datePickerOptions);
    $('.order-datepicker .datepicker').datepicker(orderDatePickerOptions);

    $('.date .input-group-addon').click(function () {
        $(this).prev().datepicker('show');
    });

    $('.input-daterange input').click(function () {
        $(this).datepicker('show');
    });
    if (Globalize && $.validator) {
        $.validator.methods.number = function (value, element) {
            return value === "" || value === "0" || Globalize.parseFloat(value);
        };
        Globalize.culture('sv-SE');
    }
    $("form:not(.do-not-check-dirty)").areYouSure({
        dirtyClass: dirty,
        message: "Du har osparade ändringar!"
    });

    $("div.autofocus > input").first().focus();

    // For buttons and anchors that ignore dirty-checks
    $(":button.do-not-check-dirty, a.do-not-check-dirty").on("click", function () {
        $("form").removeClass(dirty);
    });

    $("form.filter-form").on("change", "select, input, textarea", function (event) {
        $(this).closest("form").submit();
    });

    $(".standard-table table").DataTable({
        searching: false,
        order: [[0, 'desc']],
        language: {
            url: "//cdn.datatables.net/plug-ins/1.10.19/i18n/Swedish.json"
        }
    });

    $(".sortable-only-table").DataTable({
        paging: false,
        searching: false,
        info: false,
        dom: "rt",
        language: {
            url: "//cdn.datatables.net/plug-ins/1.10.19/i18n/Swedish.json"
        },
        "rowCallback": function (row, data) {
            $(row).addClass('table-row');
        }
    });

    $(".design-only-table").DataTable({
        paging: false,
        info: false,
        searching: false,
        sorting: false,
        language: {
            url: "//cdn.datatables.net/plug-ins/1.10.19/i18n/Swedish.json"
        }
    });

    $(".searchable-only-table").DataTable({
        paging: false,
        info: false,
        searching: true,
        sorting: false,
        language: {
            emptyTable: "Det finns inget data att visa",
            search: "Sök:",
            zeroRecords: "Sökningen gav inget resultat"
        }
    });

    var delay = function (callback, ms) {
        var timer = 0;
        return function () {
            var context = this, args = arguments;
            clearTimeout(timer);
            timer = setTimeout(function () {
                callback.apply(context, args);
            }, ms || 0);
        };
    };

    var repeatStringNumTimes = function (string, times) {
        if (times < 0) {
            return "";
        }
        if (times === 1) {
            return string;
        } else {
            return string + repeatStringNumTimes(string, times - 1);
        }
    };

    $(".ajax-listing table").each(
        function () {
            var $table = $(this);
            var $filterSelector = $table.data("filter-selector");
            var $usePaging = !$table.hasClass("no-paging");
            $.ajax({
                dataType: 'json',
                url: $table.data("ajax-column-definition"),
                success: function (json) {
                    var $columnDefinition = json;
                    var $overrideClickLinkUrlColumn = $columnDefinition.filter(function (o) {
                        return o.isOverrideClickLinkUrlColumn;
                    });
                    var $idColumn = $columnDefinition.filter(function (o) {
                        return o.isIdColumn;
                    });
                    var $leftcssDefinitionColumn = $columnDefinition.filter(function (o) {
                        return o.isLeftCssClassName;
                    });
                    $table.html("<thead><tr>" + repeatStringNumTimes("<th></th>", $columnDefinition.length) + "</tr></thead><tbody/>");
                    var $dataTable = $table.DataTable({
                        serverSide: true,
                        searching: false,
                        paging: $usePaging,
                        dom: "lrtip",
                        deferRender: true,
                        autoWidth: false,
                        createdRow: function (row, data, dataIndex) {
                            if ($table.hasClass("clickable-rows-with-action")) {
                                var $action = $table.data("click-action-url");
                                if ($overrideClickLinkUrlColumn.length > 0 && data[$overrideClickLinkUrlColumn[0].data] !== null && data[$overrideClickLinkUrlColumn[0].data] !== "") {
                                    $action = data[$overrideClickLinkUrlColumn[0].data];
                                }
                                if ($idColumn.length > 0) {
                                    var $qmark = $action.indexOf("?");
                                    if ($qmark > -1) {
                                        $action = $action.replace("?", ($action.substring($qmark - 1, $qmark) === "/" ? "" : "/") + data[$idColumn[0].data] + "?");
                                    } else {
                                        $action = $action + "/" + data[$idColumn[0].data];
                                    }
                                }
                                $(row).data("click-action-url", $action);
                            }
                            if ($leftcssDefinitionColumn.length > 0) {
                                $(row).find("td").eq(0).addClass(data[$leftcssDefinitionColumn[0].data]);
                            }
                        },
                        ajax: {
                            headers: { "RequestVerificationToken": getAntiForgeryToken() },
                            url: $table.data("ajax-path"),
                            type: 'POST',
                            data: function (data) {
                                // Read values and append to data
                                $($filterSelector + " :input").each(function () {
                                    if ($(this).is(":checkbox")) {
                                        data[$(this).prop("name")] = $(this).is(":checked") ? "true" : "false";
                                    } else {
                                        data[$(this).prop("name")] = $(this).val();
                                    }
                                });
                            },
                            error: function (jqXHR, textStatus, errorThrown) {
                                //possibly log something, but mainly reload the main page.
                                location.reload();
                            }
                        },
                        columns: $columnDefinition,
                        language: {
                            url: "//cdn.datatables.net/plug-ins/1.10.19/i18n/Swedish.json"
                        },
                        infoCallback: function (settings, start, end, max, total, pre) {
                            var $headerClass = $table.data("header-class");
                            if ($headerClass) {
                                if (total > 0) {
                                    $("." + $headerClass).text($table.data("header-text").replace("_TOTAL_", total));
                                } else {
                                    $("." + $headerClass).text($table.data("empty-header"));
                                    //Hide the table
                                    $table.hide();
                                    //Show the empty message
                                    $table.parent().append("<div class='list-empty'>" + $table.data("empty-message") + "</div>");
                                }
                            } else {
                                if (total === 0) {
                                    return $table.DataTable().i18n("sInfoEmpty");
                                } else {
                                    return $table.DataTable().i18n("sInfo").replace(/_START_/g, start).replace(/_END_/g, end).replace(/_TOTAL_/g, total);
                                }
                            }
                        }
                    });
                    $("body").on("change", $filterSelector + " select, " + $filterSelector + " input.datepicker, " + $filterSelector + " :checkbox, " + $filterSelector + " :radio", function () {
                        $dataTable.draw();
                    });
                    $("body").on("keyup", $filterSelector + " input:not(.datepicker, :checkbox, :radio)", delay(function (e) {
                        //Note keyup does not catch IE 11's x that clears the input bux, BUT if on uses the event called "input", the list is reloaded all the time, even if no changes are introduced to the input, just leaving and entering the field...
                        $dataTable.draw();
                    }, 500));
                }
            });
        });

    $("body").on("click", ".btn-datatable", function (e) {
        e.stopPropagation();
    });

    $("select:not(.dynamic-load)").each(function () {
        var allowClear = $(this).parents().hasClass("allow-clear");
        $(this).selectWoo({ minimumResultsForSearch: 10, allowClear: allowClear, language: "sv" })
            .on("select2:select select2:unselect unselect", function (e) {
                if ($(this).valid !== undefined) {
                    $(this).valid(); //jquery validation script validate on change
                }
            })
            .promise().done(function () {
                $(".select2-selection__placeholder").each(function () {
                    $(this).parent().prop("title", $(this).text());
                });
            });
    });
    $("select.dynamic-load").each(function () {
        var headers = { "RequestVerificationToken": getAntiForgeryToken() };
        var $selectBox = $(this);
        var callback = function () {
            var allowClear = $selectBox.parents().hasClass("allow-clear");
            $selectBox.selectWoo({
                allowClear: allowClear,
                language: "sv",
                ajax: {
                    url: tolkBaseUrl + $selectBox.data("search-url"),
                    delay: 250,
                    headers: headers,
                    type: 'GET',
                    dataType: 'json',
                    data: function (params) {
                        var query = {
                            search: params.term,
                            page: params.page || 1
                        }
                        return query;
                    }
                }
            });
        };
        if ($selectBox.data("initial-selection-url") !== undefined) {
            $.ajax({
                url: tolkBaseUrl + $selectBox.data("initial-selection-url"),
                type: 'Get',
                headers: headers,
                dataType: 'json',
                data: { __RequestVerificationToken: getAntiForgeryToken() },
                success: function (data) {
                    $selectBox.append(new Option(data.text, data.id, true, true));
                    callback();
                }
            });
        } else {
            callback();
        }
    });

    $("body").on("click", "table.clickable-rows-with-action > tbody > tr > td:not(.dataTables_empty)", function () {
        var $row = $(this).closest("tr");
        window.location.href = $row.data("click-action-url");
    });

    $("body").on("click", ".more-info-price", function () {
        $(this).closest(".wrap-price-info").find(".detail-price-info").collapse("toggle");
        if ($(this).text().indexOf("Visa") !== -1) {
            $(this).html($(this).html().replace("Visa", "Dölj"));
        }
        else {
            $(this).html($(this).html().replace("Dölj", "Visa"));
        }
    });

    $("body").on("click", ".table-price-toggle-price-info", function () {
        $(this).closest("tr.table-row").next("tr.table-price-row").find(".detail-price-info").collapse("toggle");
        if ($(this).text().indexOf("Visa") !== -1) {
            $(this).html($(this).html().replace("Visa", "Dölj"));
        }
        else {
            $(this).html($(this).html().replace("Dölj", "Visa"));
        }
    });

    $("body").on("click", ".collapsible-section", function () {
        var element = $(this);

        if ($(element.attr("data-target")).hasClass("in")) {
            $(element.attr("data-target")).collapse("hide");
            element.children("h2").children("span").removeClass("glyphicon-triangle-bottom");
            element.children("h2").children("span").addClass("glyphicon-triangle-right");
        }
        else {
            $(element.attr("data-target")).collapse("show");
            element.children("h2").children("span").removeClass("glyphicon-triangle-right");
            element.children("h2").children("span").addClass("glyphicon-triangle-bottom");
        }
    });

    refreshCollapsibles();
});


$(document).ready(function () {
    $('.no-auto-complete').val("");
});

$.fn.extend({
    bindEnterKey: function (input, button, context) {
        var $context = context ? $(context) : $(this);
        $(this).find(input).off('keypress');
        $(this).find(input).on('keypress', function (e) {
            if ((e.keyCode || e.which) === 13) {
                $context.find(button).click();
                e.preventDefault();
            }
        });
    },
    toggleOccasionalField: function (occasionalField, visibleOnValue) {
        var isSelected = false;
        if (Array.isArray(visibleOnValue)) {
            for (var i = 0; i < visibleOnValue.length; ++i) {
                if ($(this).hasValue(visibleOnValue[i])) {
                    isSelected = true;
                    break;
                }
            }
        } else {
            isSelected = $(this).hasValue(visibleOnValue);
        }

        if (isSelected) {
            occasionalField.show(200);
        } else {
            occasionalField.hide(200);
        }
    },
    hasValue: function (value) {
        //Intentional use of == instead of === since "1" and 1 needs to be seen as equal..
        return $(this).is(":checkbox")
            ? $(this).is(":checked") === value
            : $(this).val() == value; //eslint-disable-line eqeqeq
    },
    disable: function () {
        $(this).attr("disabled", "disabled");
    },
    enable: function () {
        $(this).removeAttr("disabled");
    },
    disableOnSubmit: function () {
        $(this).disable();
        var isValid = $(this).closest("form").valid();
        if (!isValid) {
            $(this).enable();
        }
    },
    // wizard
    tolkWizard: function (opts) {
        var $wizard = $(".wizard");
        var options = $.extend({}, opts);
        opts.validationFalseHandler = function (result) { result.focusInvalid(); };
        opts.onloadHandler = function () {
            $wizard.find(".wizard-step:not(.wizard-step-hidden)").each(function () {
                if (options.onloadHandler !== undefined) {
                    options.onloadHandler();
                }
            });
        };
        $wizard.wizardFormValidation(opts);
    },
    openDialog: function () {
        $(this).find("input:not(:checkbox,:hidden),select, textarea").val("");
        $(this).find("input:checkbox").prop("checked", false);
        var $form = $(this).find('form:first');
        $(this).bindEnterKey('form:first input', '.btn-default');
        $form.find(".field-validation-error")
            .addClass("field-validation-valid")
            .removeClass("field-validation-error").html("");
        $(this).modal({ backdrop: "static" });
    },
});
