'use strict';

(function () {
  var ApiUrl = 'https://api.awesomes.cn'
  Vue.component('vue-item', {
    props: ['jsondata', 'theme'],
    template: '#item-template'
  })

  Vue.component('vue-outer', {
    props: ['jsondata', 'isend', 'theme'],
    template: '#outer-template'
  })

  Vue.component('vue-expand', {
    props: [],
    template: '#expand-template'
  })

  Vue.component('vue-val', {
    props: ['field', 'val', 'isend', 'theme'],
    template: '#val-template'
  })

  Vue.use({
    install: function (Vue, options) {
      
      // 判断数据类型
      Vue.prototype.getTyp = function (val) {
        return toString.call(val).split(']')[0].split(' ')[1]
      }

      // 判断是否是对象或者数组，以对下级进行渲染
      Vue.prototype.isObjectArr = function (val) {
        return ['Object', 'Array'].indexOf(this.getTyp(val)) > -1
      }

      // 折叠
      Vue.prototype.fold = function ($event) {
        var target = Vue.prototype.expandTarget($event)
        target.siblings('svg').show()
        target.hide().parent().siblings('.expand-view').hide()
        target.parent().siblings('.fold-view').show()
      }
      // 展开
      Vue.prototype.expand = function ($event) {
        var target = Vue.prototype.expandTarget($event)
        target.siblings('svg').show()
        target.hide().parent().siblings('.expand-view').show()
        target.parent().siblings('.fold-view').hide()
      }

      //获取展开折叠的target
      Vue.prototype.expandTarget = function ($event) {
        switch($event.target.tagName.toLowerCase()) {
          case 'use':
            return $($event.target).parent()
          case 'label':
            return $($event.target).closest('.fold-view').siblings('.expand-wraper').find('.icon-square-plus').first()
          default:
            return $($event.target)
        }
      }

      // 格式化值
      Vue.prototype.formatVal = function (val) { 
        switch(Vue.prototype.getTyp(val)) {
          case 'String':
            return '"' + val + '"'
            break

          case 'Null': 
            return 'null'
            break

          default:
            return val

        }
      }

      // 判断值是否是链接
      Vue.prototype.isaLink = function (val) {
        return /^((https|http|ftp|rtsp|mms)?:\/\/)[^\s]+/.test(val)
      }

      // 计算对象的长度
      Vue.prototype.objLength = function (obj) { 
        return Object.keys(obj).length
      }
    }
  })

    var initJson = {
        "[]": {
            "page": 1,
            "count": 2,
            "Moment": {
                "content$": "%a%"
            },
            "User": {
                "id@": "/Moment/userId",
                "@column": "id,name,head"
            },
            "Comment[]": {
                "count": 2,
                "Comment": {
                    "momentId@": "[]/Moment/id"
                }
            }
        }
    };

  // 主题 [key, String, Number, Boolean, Null, link-link, link-hover]
  let themes = [
    ['#92278f', '#3ab54a', '#25aae2', '#f3934e', '#f34e5c', '#717171'],
    ['rgb(19, 158, 170)', '#cf9f19', '#ec4040', '#7cc500', 'rgb(211, 118, 126)', 'rgb(15, 189, 170)'],
    ['#886', '#25aae2', '#e60fc2', '#f43041', 'rgb(180, 83, 244)', 'rgb(148, 164, 13)'],
    ['rgb(97, 97, 102)', '#cf4c74', '#20a0d5', '#cd1bc4', '#c1b8b9', 'rgb(25, 8, 174)']
  ]
  var App = new Vue({
    el: '#app',
    data: {
      baseview: 'formater',
      view: 'code',
        jsoncon: JSON.stringify(initJson, null, '    ') ,
      newjsoncon: '{"name": "Json on"}',
      jsonhtml: (initJson),
      compressStr: '',
      error: {},
      historys: [],
      history: {name: ''},
      isSaveShow: false,
      isExportTxtShow: false,
      exTxt: {
        name: 'JSONON'
      },
      themes: themes,
      checkedTheme: 0,
      shareKey: '', // 分享后返回的ID
      isSharing: false
    },
      methods: {
          hpost: function () {
              $.ajax({
                  url: $('#rest-url').val(),
                  type: "POST", dataType: "json",
                  contentType: "application/json;charset=utf-8",
                  data: $('#vInput').val(),//JSON.stringify($('#vInput').val()),
                  success: function (data) {
                  
                      App.jsonhtml = data;
                      App.view = 'code';
                  },
                  error: function () {
                      alert('Something went wrong, double-check the URL and callback parameter.');
                  }
              });
          },
      // 全部展开
      expandAll: function () {
        $('.icon-square-min').show()
        $('.icon-square-plus').hide()
        $('.expand-view').show()
        $('.fold-view').hide()
      },

      // 全部折叠
      collapseAll: function () {
        $('.icon-square-min').hide()
        $('.icon-square-plus').show()
        $('.expand-view').hide()
        $('.fold-view').show()
      },

      // 压缩
      compress: function () {
        App.jsoncon = Parse.compress(App.jsoncon)
      },

      // diff
      diffTwo: function () {
        var oldJSON = {}
        var newJSON = {}
        App.view = 'code'
        try {
          oldJSON = jsonlint.parse(App.jsoncon)
        } catch (ex) {
          App.view = 'error'
          App.error = {
            msg: '原 JSON 解析错误\r\n' + ex.message
          }
          return
        }

        try {
          newJSON = jsonlint.parse(App.newjsoncon)
        } catch (ex) {
          App.view = 'error'
          App.error = {
            msg: '新 JSON 解析错误\r\n' + ex.message
          }
          return
        }

        var base = difflib.stringAsLines(JSON.stringify(oldJSON, '', 4))
        var newtxt = difflib.stringAsLines(JSON.stringify(newJSON, '', 4))
        var sm = new difflib.SequenceMatcher(base, newtxt)
        var opcodes = sm.get_opcodes()
        $('#diffoutput').empty().append(diffview.buildView({
          baseTextLines: base,
          newTextLines: newtxt,
          opcodes: opcodes,
          baseTextName: '原 JSON',
          newTextName: '新 JSON',
          contextSize: 2,
          viewType: 0
        }))
      },

      // 清空
      clearAll: function () {
        App.jsoncon = ''
      },

      // 美化
      beauty: function () {
        App.jsoncon = JSON.stringify(JSON.parse(App.jsoncon), '', 4)
      },

      baseViewToDiff: function () {
        App.baseview = 'diff'
        App.diffTwo()
      },

      // 回到格式化视图
      baseViewToFormater: function () {
        App.baseview = 'formater'
        App.view = 'code'
        App.showJsonView()
      },

      // 根据json内容变化格式化视图
      showJsonView: function () {
        if (App.baseview === 'diff') {
          return
        }
        try {
          if (this.jsoncon.trim() === '') {
            App.view = 'empty'
          } else {
            App.view = 'code'
            App.jsonhtml = jsonlint.parse(this.jsoncon)
          }
        } catch (ex) {
          App.view = 'error'
          App.error = {
            msg: ex.message
          }
        }
      },

      // 保存当前的JSON
      save: function () {
        if (App.history.name.trim() === '') {
          Helper.alert('名称不能为空！', 'danger')
          return
        }
        var val = {
          name: App.history.name,
          data: App.jsoncon
        }
        var key = String(Date.now())
        localforage.setItem(key, val, function (err, value) {
          Helper.alert('保存成功！', 'success')
          App.isSaveShow = false
          val.key = key
          App.historys.push(val)
        })
      },

      // 删除已保存的
      remove: function (item, index) {
        localforage.removeItem(item.key, function () {
          App.historys.splice(index, 1)
        })
      },

      // 根据历史恢复数据
      restore: function (item) {
        localforage.getItem(item.key, function (err, value) {
          App.jsoncon = item.data
        })
      },

      // 获取所有保存的json
      listHistory: function () {
        localforage.iterate(function (value, key, iterationNumber) {
          if (key[0] !== '#') {
            value.key = key
            App.historys.push(value)
          }
          if (key === '#theme') {
            // 设置默认主题
            App.checkedTheme = value
          }
        })
      },

      // 导出文本
      exportTxt: function () {
        saveTextAs(App.jsoncon, App.exTxt.name + '.txt')
        App.isExportTxtShow = false
      },

      // 切换主题
      switchTheme: function (index) {
        this.checkedTheme = index
        localforage.setItem('#theme', index)
      },

      // 获取分享的链接
      shareUrl: function (key) {
        return `${window.location.origin}?key=${key}`
      },

      // 分享
      share: function () {
        let con = App.jsoncon
        if (con.trim() === '') {
          return
        }
        App.isSharing = true
        $.ajax({
          type: 'POST',
          url: `${ApiUrl}/json`,
          contentType: 'application/json; charset=utf-8',
          data: JSON.stringify({con: con, key: App.shareKey}),
          success: (data) => {
            App.isSharing = false
            App.shareKey = uuidv1()
            if (data.status) {
              Helper.alert('分享成功，已将链接复制到剪贴板，只能保存24小时', 'success')
            } else {
            }
          }
        })
      }
    },
    watch: {
      jsoncon: function () {
        App.showJsonView()
      }
    },
    computed: {
      theme: function () {
        let th = this.themes[this.checkedTheme]
        let result = {}
        let index = 0
        ;['key', 'String', 'Number', 'Boolean', 'Null', 'link-link'].forEach(key => {
          result[key] = th[index]
          index++
        })
        return result
      }
    },
    created () {
      this.listHistory()
      var clipboard = new Clipboard('.copy-btn')
      let sps = window.location.href.split('?key=')
      let jsonID = sps[sps.length - 1]
      this.shareKey = uuidv1()
      if (sps.length > 1 && jsonID.length > 5) {
        $.get(`${ApiUrl}/json?key=${jsonID}`, function (data) {
          if (data.status) {
            App.jsoncon = data.item.con
          }
        })
      }
    }
  })
})()
